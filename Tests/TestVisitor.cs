using Xunit;
using FluentAssertions;
using LoxInterpreter.RazerLox;

namespace Tests
{
    public class TestVisitor
    {
        [Fact]
        public void CanGetParenthesisTree()
        {
            // assemble
            var subject = new AstPrinter();

            AExpression expr =
                new BinaryExpression(
                    new UnaryExpression(new Token(TokenType.MINUS, "-", null, 1),
                    new LiteralExpression(123)),
                new Token(TokenType.STAR, "*", null, 1),
                new GroupingExpression(new LiteralExpression(45.67)));

            // act
            var result = subject.GetParenthesizedString(expr);

            // assert
            result.Should().BeEquivalentTo("(* (- 123) (group 45.67))");
        }
    }
}
