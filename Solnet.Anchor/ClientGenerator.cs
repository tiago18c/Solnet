using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Models;
using Solnet.Anchor.Models.Accounts;
using Solnet.Anchor.Models.Types;
using Solnet.Anchor.Models.Types.Base;
using Solnet.Anchor.Models.Types.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solnet.Anchor
{
    public static class ClientGeneratorDefaultValues
    {
        public static AccessorListSyntax PropertyAccessorList { get; set; } = AccessorList(List<AccessorDeclarationSyntax>()
            .Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
            .Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))));

        public static SyntaxTokenList PublicModifier = TokenList(Token(SyntaxKind.PublicKeyword));

        public static SyntaxTokenList PublicStaticModifiers = TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));

        public static SyntaxToken OpenBraceToken = Token(SyntaxKind.OpenBraceToken);

    }

    public class ClientGenerator
    {

        public void GenerateSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> members = new();

            if (idl.Accounts != null)
                members.Add(GenerateAccountsSyntaxTree(idl));

            if (idl.Errors != null)
                members.Add(GenerateErrorsSyntaxTree(idl));

            if (idl.Events != null)
                members.Add(GenerateEventsSyntaxTree(idl));

            if (idl.Types != null)
                members.Add(GenerateTypesSyntaxTree(idl));

            //members.Add(GenerateClientSyntaxTree(idl));

            members.Add(GenerateProgramSyntaxTree(idl));

            var st = SyntaxTree(NamespaceDeclaration(ParseName(idl.Name.ToPascalCase()))
                .AddMembers(members.ToArray()));

            var res = st.GetRoot().NormalizeWhitespace().ToFullString();


            res.ToPascalCase();
        }

        private MemberDeclarationSyntax GenerateProgramSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> classes = new();
            List<MemberDeclarationSyntax> instructions = new();

            foreach (var instr in idl.Instructions)
            {
                classes.AddRange(GenerateAccountsClassSyntaxTree(instr.Accounts, instr.Name.ToPascalCase()));
                instructions.Add(GenerateInstructionSerializationSyntaxTree(idl.Types, instr));
            }

            classes.Add(ClassDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicStaticModifiers, Identifier(idl.Name.ToPascalCase() + "Program"), null, null, List<TypeParameterConstraintClauseSyntax>(), List(instructions)));


            return NamespaceDeclaration(IdentifierName(idl.Name.ToPascalCase() + ".Program"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), List(classes));
        }

        private List<ExpressionSyntax> GenerateKeysInitExpressions(IIdlAccountItem[] accounts, ExpressionSyntax identifierNameSyntax)
        {
            List<ExpressionSyntax> initExpressions = new();


            foreach (var acc in accounts)
            {
                if (acc is IdlAccounts mulAccs)
                {
                    initExpressions.AddRange(GenerateKeysInitExpressions(
                        mulAccs.Accounts,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            identifierNameSyntax,
                            IdentifierName(mulAccs.Name.ToPascalCase()))));
                }
                else if (acc is IdlAccount singleAcc)
                {
                    initExpressions.Add(InvocationExpression(
                        MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("AccountMeta"),
                        IdentifierName(singleAcc.IsMut ? "Writable" : "ReadOnly")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            identifierNameSyntax,
                            IdentifierName(singleAcc.Name.ToPascalCase()))),
                        Argument(LiteralExpression(singleAcc.IsSigner ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
                    }))));

                }
            }
            return initExpressions;
        }

        private MemberDeclarationSyntax GenerateInstructionSerializationSyntaxTree(IIdlTypeDefinitionTy[] definedTypes, IdlInstruction instr)
        {
            List<ParameterSyntax> parameters = new();
            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("PublicKey"), Identifier("programId"), null));
            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName(instr.Name.ToPascalCase() + "Accounts"), Identifier("accounts"), null));

            foreach (var arg in instr.Args)
            {
                parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), GetTypeSyntax(arg.Type), Identifier(arg.Name), null));

            }



            List<ExpressionSyntax> initExprs = new();

            initExprs.AddRange(GenerateKeysInitExpressions(instr.Accounts, IdentifierName("accounts")));


            List<StatementSyntax> body = new();

            var initExpr = InitializerExpression(SyntaxKind.CollectionInitializerExpression, ClientGeneratorDefaultValues.OpenBraceToken, SeparatedList<SyntaxNode>(initExprs), Token(SyntaxKind.CloseBraceToken));

            body.Add(LocalDeclarationStatement(VariableDeclaration(
                GenericName(Identifier("List"), TypeArgumentList(SeparatedList(new TypeSyntax[] { IdentifierName("AccountMeta") }))),
                SingletonSeparatedList(VariableDeclarator(Identifier("keys"), null,
                EqualsValueClause(ImplicitObjectCreationExpression(ArgumentList(), initExpr)))))));

            body.Add(LocalDeclarationStatement(VariableDeclaration(
                ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())),
                SingletonSeparatedList(VariableDeclarator(Identifier("data"),
                    null,
                    EqualsValueClause(ArrayCreationExpression(
                        ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)),
                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1200)))))))))))));

            body.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))))));


            body.Add(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("data"), IdentifierName("WriteU64")),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(SigHash.GetSigHash(instr.Name, "global")))),
                    Argument(IdentifierName("offset"))
                })))));

            body.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(8)))));


            foreach (var arg in instr.Args)
            {
                body.AddRange(GenerateArgSerializationSyntaxList(definedTypes, arg.Type, IdentifierName(arg.Name)));
            }



            body.Add(LocalDeclarationStatement(VariableDeclaration(
                ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())),
                SingletonSeparatedList(VariableDeclarator(Identifier("resultData"),
                    null,
                    EqualsValueClause(ArrayCreationExpression(
                        ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)),
                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(IdentifierName("offset"))))))))))));

            body.Add(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Array"), IdentifierName("Copy")),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(IdentifierName("data")),
                    Argument(IdentifierName("resultData")),
                    Argument(IdentifierName("offset"))
                })))));

            body.Add(ReturnStatement(ObjectCreationExpression(IdentifierName("TransactionInstruction"), null,
                InitializerExpression(SyntaxKind.ObjectInitializerExpression, SeparatedList(new ExpressionSyntax[]
                {
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("Keys"), IdentifierName("keys") ),
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("ProgramId"),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("programId"), IdentifierName("KeyBytes"))),

                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("Data"), IdentifierName("resultData")),

                })))));


            return MethodDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicStaticModifiers, IdentifierName("TransactionInstruction"), null, Identifier(instr.Name.ToPascalCase()), null, ParameterList(SeparatedList(parameters)), List<TypeParameterConstraintClauseSyntax>(), Block(body), null);
        }

        private bool IsSimpleEnum(IIdlTypeDefinitionTy[] types, string name)
        {
            var res = types.FirstOrDefault(x => x.Name == name);

            if (res is EnumIdlTypeDefinition enumDef)
            {
                return enumDef.Variants.All(x => x is SimpleEnumVariant);
            }
            return false;
        }

        private IEnumerable<StatementSyntax> GenerateArgSerializationSyntaxList(IIdlTypeDefinitionTy[] definedTypes, IIdlType type, ExpressionSyntax identifierNameSyntax)
        {
            List<StatementSyntax> syntaxes = new();

            if (type is IdlDefined definedType)
            {
                if (!IsSimpleEnum(definedTypes, definedType.TypeName))
                {
                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"), InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                identifierNameSyntax,
                                IdentifierName("Serialize")),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                            Argument(IdentifierName("data")),
                            Argument(IdentifierName("offset"))
                            }))))));
                }
                else
                {
                    syntaxes.Add(ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("data"),
                            IdentifierName("WriteU8")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                        Argument(CastExpression(PredefinedType(Token(SyntaxKind.ByteKeyword)), identifierNameSyntax)),
                        Argument(IdentifierName("offset"))
                        })))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));
                }
            }
            else if (type is IdlValueType valueType)
            {
                var (serializerFunctionName, typeSize) = GetSerializationValuesForValueType(valueType);

                syntaxes.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("data"),
                        serializerFunctionName),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(identifierNameSyntax),
                        Argument(IdentifierName("offset"))
                    })))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(typeSize)))));
            }
            else if (type is IdlString str)
            {
                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("data"),
                            IdentifierName("WriteString")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(identifierNameSyntax),
                            Argument(IdentifierName("offset"))
                        }))))));
            }
            else if (type is IdlArray arr)
            {
                var varIdIdentifier = Identifier(identifierNameSyntax.ToString().ToCamelCase() + "Element");
                var varIdExpression = IdentifierName(varIdIdentifier);

                if (!arr.Size.HasValue)
                {
                    syntaxes.Add(ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("data"),
                            IdentifierName("WriteU32")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                identifierNameSyntax,
                                IdentifierName("Length"))),
                            Argument(IdentifierName("offset"))
                        })))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(4)))));
                }

                // need to create different serialization for u8 arr

                if (arr.ValuesType is IdlValueType innerType && (innerType.TypeName == "u8"))
                {
                    syntaxes.Add(ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("data"),
                            IdentifierName("WriteSpan")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(identifierNameSyntax),
                            Argument(IdentifierName("offset"))
                        })))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, identifierNameSyntax, IdentifierName("Length")))));
                }
                else
                {
                    var foreachBlockContent = Block(GenerateArgSerializationSyntaxList(definedTypes, arr.ValuesType, varIdExpression));

                    syntaxes.Add(ForEachStatement(IdentifierName("var"), varIdIdentifier, identifierNameSyntax, foreachBlockContent));

                }

            }
            else if (type is IdlBigInt bi)
            {
                bool isUnsigned = bi.TypeName == "u128";

                var conditionBody = ThrowStatement(ObjectCreationExpression(
                    IdentifierName("OverflowException"),
                    ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(BinaryExpression(
                        SyntaxKind.AddExpression,
                        InvocationExpression(
                            IdentifierName("nameof"),
                            ArgumentList(SingletonSeparatedList(Argument(identifierNameSyntax)))),
                        LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(" current value uses more than 16 bytes.")))))),
                    null));

                syntaxes.Add(IfStatement(
                    BinaryExpression(SyntaxKind.GreaterThanExpression, InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            identifierNameSyntax,
                            IdentifierName("GetByteCount")),
                        ArgumentList(SingletonSeparatedList(Argument(LiteralExpression(isUnsigned ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))))),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(16))),
                    conditionBody));


                syntaxes.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        identifierNameSyntax,
                        IdentifierName("TryWriteBytes")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("data"),
                                IdentifierName("AsSpan")),
                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName("offset")))))),
                        Argument(IdentifierName("out _ ")),
                        Argument(LiteralExpression(isUnsigned ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
                    })))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(16)))));
            }
            else if (type is IdlPublicKey)
            {
                syntaxes.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("data"),
                        IdentifierName("WritePubKey")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                            Argument(identifierNameSyntax),
                            Argument(IdentifierName("offset"))
                    })))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(32)))));
            }
            else if (type is IdlOptional optionalType)
            {
                var condition = BinaryExpression(SyntaxKind.NotEqualsExpression, identifierNameSyntax, LiteralExpression(SyntaxKind.NullLiteralExpression));

                List<StatementSyntax> conditionBody = new();

                conditionBody.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("data"),
                        IdentifierName("WriteU8")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralToken, Literal(1))),
                        Argument(IdentifierName("offset"))
                    })))));

                conditionBody.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));

                conditionBody.AddRange(GenerateArgSerializationSyntaxList(definedTypes, optionalType.ValuesType, identifierNameSyntax));

                var elseBody = Block(
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("data"),
                            IdentifierName("WriteU8")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(LiteralExpression(SyntaxKind.NumericLiteralToken, Literal(0))),
                            Argument(IdentifierName("offset"))
                        })))),

                    ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));


                syntaxes.Add(IfStatement(
                    condition,
                    Block(conditionBody),
                    ElseClause(elseBody)));




            }
            else
            {
                throw new Exception("Unexpected type " + type.GetType().FullName);
            }

            return syntaxes;
        }

        private (IdentifierNameSyntax, int) GetSerializationValuesForValueType(IdlValueType valueType)
            => valueType.TypeName switch
            {
                "u8" => (IdentifierName("WriteU8"), 1),
                "i8" => (IdentifierName("WriteS8"), 1),
                "u16" => (IdentifierName("WriteU16"), 2),
                "i16" => (IdentifierName("WriteS16"), 2),
                "u32" => (IdentifierName("WriteU32"), 4),
                "i32" => (IdentifierName("WriteS32"), 4),
                "u64" => (IdentifierName("WriteU64"), 8),
                "i64" => (IdentifierName("WriteS64"), 8),
                _ => (IdentifierName("WriteBool"), 1)
            };

        private List<MemberDeclarationSyntax> GenerateAccountsClassSyntaxTree(IIdlAccountItem[] accounts, string v)
        {
            List<MemberDeclarationSyntax> classes = new();
            List<MemberDeclarationSyntax> currentClassMembers = new();

            foreach (var acc in accounts)
            {
                if (acc is IdlAccount singleAcc)
                {
                    currentClassMembers.Add(PropertyDeclaration(
                        List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        IdentifierName("PublicKey"),
                        default,
                        Identifier(singleAcc.Name.ToPascalCase()),
                        ClientGeneratorDefaultValues.PropertyAccessorList));
                }
                else if (acc is IdlAccounts multipleAccounts)
                {
                    classes.AddRange(GenerateAccountsClassSyntaxTree(multipleAccounts.Accounts, v + multipleAccounts.Name.ToPascalCase()));

                    currentClassMembers.Add(PropertyDeclaration(
                        List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        IdentifierName(v + multipleAccounts.Name.ToPascalCase() + "Accounts"),
                        default,
                        Identifier(multipleAccounts.Name.ToPascalCase()),
                        ClientGeneratorDefaultValues.PropertyAccessorList));
                }
            }

            classes.Add(ClassDeclaration(
                List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicModifier,
                Identifier(v + "Accounts"),
                null,
                null,
                List<TypeParameterConstraintClauseSyntax>(),
                List(currentClassMembers)));

            return classes;
        }

        private MemberDeclarationSyntax GenerateClientSyntaxTree(Idl idl)
        {
            throw new NotImplementedException();
        }

        private MemberDeclarationSyntax GenerateTypesSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> types = new();

            for (int i = 0; i < idl.Types.Length; i++)
            {
                types.AddRange(GenerateTypeDeclaration(idl, idl.Types[i], true));
            }

            return NamespaceDeclaration(IdentifierName(idl.Name.ToPascalCase() + ".Types"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), List(types));
        }

        private SyntaxList<MemberDeclarationSyntax> GenerateTypeDeclaration(Idl idl, IIdlTypeDefinitionTy idlTypeDefinitionTy, bool generateSerialization)
            => idlTypeDefinitionTy switch
            {
                StructIdlTypeDefinition structIdl => GenerateClassDeclaration(idl, structIdl, generateSerialization),
                EnumIdlTypeDefinition enumIdl => GenerateEnumDeclaration(idl, enumIdl, generateSerialization),
                _ => throw new Exception("bad type")
            };

        private TypeSyntax GetTypeSyntax(IIdlType type)
            => type switch
            {
                IdlArray arr => ArrayType(GetTypeSyntax(arr.ValuesType), SingletonList(ArrayRankSpecifier())),
                IdlBigInt => IdentifierName("BigInteger"),
                IdlDefined def => IdentifierName(def.TypeName),
                IdlOptional opt => NullableType(GetTypeSyntax(opt.ValuesType)),
                IdlPublicKey => IdentifierName("PublicKey"),
                IdlString => PredefinedType(Token(SyntaxKind.StringKeyword)),
                IdlValueType v => PredefinedType(Token(GetTokenForValueType(v))),
                _ => throw new Exception("huh wat")
            };

        private SyntaxKind GetTokenForValueType(IdlValueType idlValueType)
            => idlValueType.TypeName switch
            {
                "u8" => SyntaxKind.ByteKeyword,
                "i8" => SyntaxKind.SByteKeyword,
                "u16" => SyntaxKind.UShortKeyword,
                "i16" => SyntaxKind.ShortKeyword,
                "u32" => SyntaxKind.UIntKeyword,
                "i32" => SyntaxKind.IntKeyword,
                "u64" => SyntaxKind.ULongKeyword,
                "i64" => SyntaxKind.LongKeyword,
                _ => SyntaxKind.BoolKeyword
            };

        private SyntaxList<MemberDeclarationSyntax> GenerateClassDeclaration(Idl idl, StructIdlTypeDefinition structIdl, bool generateSerialization)
        {
            List<MemberDeclarationSyntax> classMembers = new();

            foreach (var field in structIdl.Fields)
            {
                classMembers.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, GetTypeSyntax(field.Type), default, Identifier(field.Name.ToPascalCase()), ClientGeneratorDefaultValues.PropertyAccessorList));
            }

            List<StatementSyntax> body = new();
            if (generateSerialization)
            {
                body.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("offset"),
                    IdentifierName("initialOffset"))));


                foreach (var field in structIdl.Fields)
                {
                    body.AddRange(GenerateArgSerializationSyntaxList(idl.Types, field.Type, IdentifierName(field.Name.ToPascalCase())));
                }

                body.Add(ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));


                classMembers.Add(MethodDeclaration(List<AttributeListSyntax>(),
                    ClientGeneratorDefaultValues.PublicModifier,
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    null,
                    Identifier("Serialize"),
                    null,
                    ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())), Identifier("data"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),

                    })),
                    List<TypeParameterConstraintClauseSyntax>(),
                    Block(body),
                    null));
            }

            return SingletonList<MemberDeclarationSyntax>(ClassDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, Identifier(structIdl.Name.ToPascalCase()), null, null, List<TypeParameterConstraintClauseSyntax>(), List(classMembers)));
        }

        private SyntaxList<MemberDeclarationSyntax> GenerateEnumDeclaration(Idl idl, EnumIdlTypeDefinition enumIdl, bool generateSerialization)
        {
            List<EnumMemberDeclarationSyntax> enumMembers = new();
            List<MemberDeclarationSyntax> supportClasses = new();
            List<MemberDeclarationSyntax> mainClassProperties = new();

            List<SwitchSectionSyntax> serializationCases = new();




            //switchstatement
            //  switchsection
            //    switchlabel - expression
            //    block - body (stmt, breakstmt)
            //

            foreach (var member in enumIdl.Variants)
            {
                enumMembers.Add(EnumMemberDeclaration(member.Name));

                List<StatementSyntax> caseStatements = new();

                if (member is TupleFieldsEnumVariant tuple)
                {
                    List<TypeSyntax> typeSyntaxes = new();
                    List<StatementSyntax> tupleSerializationStatements = new();

                    for (int i = 0; i < tuple.Fields.Length; i++)
                    {
                        typeSyntaxes.Add(GetTypeSyntax(tuple.Fields[i]));
                        caseStatements.AddRange(GenerateArgSerializationSyntaxList(
                            idl.Types,
                            tuple.Fields[i],
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(member.Name.ToPascalCase() + "Value"), IdentifierName("Item" + (i + 1)))));
                    }

                    mainClassProperties.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, GenericName(Identifier("Tuple"), TypeArgumentList(SeparatedList(typeSyntaxes))), default, Identifier(member.Name.ToPascalCase() + "Value"), ClientGeneratorDefaultValues.PropertyAccessorList));

                }
                else if (member is NamedFieldsEnumVariant structVariant)
                {
                    List<MemberDeclarationSyntax> fields = new();
                    List<StatementSyntax> body = new();

                    body.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("offset"),
                        IdentifierName("initialOffset"))));


                    foreach (var field in structVariant.Fields)
                    {
                        fields.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, GetTypeSyntax(field.Type), default, Identifier(field.Name.ToPascalCase()), ClientGeneratorDefaultValues.PropertyAccessorList));
                    }



                    foreach (var field in structVariant.Fields)
                    {
                        body.AddRange(GenerateArgSerializationSyntaxList(idl.Types, field.Type, IdentifierName(field.Name.ToPascalCase())));
                    }

                    body.Add(ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));


                    fields.Add(MethodDeclaration(List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        null,
                        Identifier("Serialize"),
                        null,
                        ParameterList(SeparatedList(new ParameterSyntax[] {
                            Parameter(List<AttributeListSyntax>(), TokenList(), ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())), Identifier("data"), null),
                            Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),
                        })),
                        List<TypeParameterConstraintClauseSyntax>(),
                        Block(body),
                        null));

                    caseStatements.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"), InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(member.Name.ToPascalCase() + "Value"),
                                IdentifierName("Serialize")),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                            Argument(IdentifierName("data")),
                            Argument(IdentifierName("offset"))
                            }))))));




                    supportClasses.Add(ClassDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, Identifier(member.Name.ToPascalCase() + "Type"), null, null, List<TypeParameterConstraintClauseSyntax>(), List(fields)));

                    mainClassProperties.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, IdentifierName(member.Name.ToPascalCase() + "Type"), default, Identifier(member.Name.ToPascalCase() + "Value"), ClientGeneratorDefaultValues.PropertyAccessorList));

                }

                if (caseStatements.Count > 0)
                {
                    caseStatements.Add(BreakStatement());
                    serializationCases.Add(SwitchSection(SingletonList<SwitchLabelSyntax>(CaseSwitchLabel(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(enumIdl.Name.ToPascalCase() + "Type"), IdentifierName(member.Name)))),
                        List(caseStatements)));
                }
            }

            if (mainClassProperties.Count == 0)
            {
                return SingletonList<MemberDeclarationSyntax>(EnumDeclaration(
                    List<AttributeListSyntax>(),
                    ClientGeneratorDefaultValues.PublicModifier,
                    Identifier(enumIdl.Name.ToPascalCase()),
                    BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(PredefinedType(Token(SyntaxKind.ByteKeyword))))),
                    SeparatedList(enumMembers)));
            }

            // need to create specific serialization

            var ser = Block(
                ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression, 
                    IdentifierName("offset"), 
                    IdentifierName("initialOffset"))),
                
                ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("data"),
                        IdentifierName("WriteU8")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(CastExpression(PredefinedType(Token(SyntaxKind.ByteKeyword)), IdentifierName("Type"))),
                        Argument(IdentifierName("offset"))
                    })))),

                ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))),

                SwitchStatement(IdentifierName("Type"), List(serializationCases)),

                ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));

            mainClassProperties.Add(MethodDeclaration(List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        null,
                        Identifier("Serialize"),
                        null,
                        ParameterList(SeparatedList(new ParameterSyntax[] {
                            Parameter(List<AttributeListSyntax>(), TokenList(), ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())), Identifier("data"), null),
                            Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),
                        })),
                        List<TypeParameterConstraintClauseSyntax>(),
                        ser,
                        null));

            mainClassProperties.Add(PropertyDeclaration(List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicModifier,
                IdentifierName(enumIdl.Name.ToPascalCase() + "Type"),
                default,
                Identifier("Type"),
                ClientGeneratorDefaultValues.PropertyAccessorList));

            supportClasses.Add(ClassDeclaration(List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicModifier,
                Identifier(enumIdl.Name.ToPascalCase()),
                null,
                null,
                List<TypeParameterConstraintClauseSyntax>(),
                List(mainClassProperties)));

            return SingletonList<MemberDeclarationSyntax>(EnumDeclaration(
                List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicModifier,
                Identifier(enumIdl.Name.ToPascalCase() + "Type"),
                BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(PredefinedType(Token(SyntaxKind.ByteKeyword))))),
                SeparatedList(enumMembers))).AddRange(supportClasses);
        }

        private MemberDeclarationSyntax GenerateEventsSyntaxTree(Idl idl)
        {
            SyntaxList<MemberDeclarationSyntax> events = List<MemberDeclarationSyntax>();

            for (int i = 0; i < idl.Events.Length; i++)
            {


            }

            return NamespaceDeclaration(IdentifierName(idl.Name.ToPascalCase() + ".Events"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), events);
        }

        private MemberDeclarationSyntax GenerateErrorsSyntaxTree(Idl idl)
        {
            SyntaxList<MemberDeclarationSyntax> errors = List<MemberDeclarationSyntax>();

            for (int i = 0; i < idl.Errors.Length; i++)
            {


            }

            return NamespaceDeclaration(IdentifierName(idl.Name.ToPascalCase() + ".Errors"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), errors);
        }

        private MemberDeclarationSyntax GenerateAccountsSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> accounts = new();

            for (int i = 0; i < idl.Accounts.Length; i++)
            {
                accounts.AddRange(GenerateTypeDeclaration(idl, idl.Accounts[i], false));
            }

            return NamespaceDeclaration(IdentifierName(idl.Name.ToPascalCase() + ".Accounts"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), List(accounts));
        }

    }
}