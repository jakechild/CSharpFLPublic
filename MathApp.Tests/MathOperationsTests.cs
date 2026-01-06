namespace MathApp.Tests
{
    public class MathOperationsTests
    {
        #region Addition Tests
        [Fact]
        public void AdditionTest()
        {
            int result = MathApp.MathOperations.Add(5, 3);
            Assert.Equal(8, result);
        }

        [Fact]
        public void AdditionTest2()
        {
            int result = MathApp.MathOperations.Add(4, 3);
            Assert.Equal(7, result);
        }

        [Fact]
        public void AdditionTest3()
        {
            int result = MathApp.MathOperations.Add(7, 3);
            Assert.Equal(10, result);
        }

        #endregion

        #region Division Tests
        [Fact]
        public void DivisionTest()
        {
            double result = MathApp.MathOperations.Divide(10, 2);
            Assert.Equal(5.0, result);
        }

        [Fact]
        public void DivisionTest2()
        {
            double result = MathApp.MathOperations.Divide(15, 2);
            Assert.Equal(7.5, result);
        }

        [Fact]
        public void DivisionTest3()
        {
            double result = MathApp.MathOperations.Divide(150, 2);
            Assert.Equal(75.0, result);
        }

        #endregion

        #region Multiplication Tests
        [Fact]
        public void MultiplicationTest()
        {
            double result = MathApp.MathOperations.Multiply(10, 2);
            Assert.Equal(20.0, result);
        }

        [Fact]
        public void MultiplicationTest2()
        {
            double result = MathApp.MathOperations.Multiply(15, 2);
            Assert.Equal(30.0, result);
        }

        [Fact]
        public void MultiplicationTest3()
        {
            double result = MathApp.MathOperations.Multiply(150, 2);
            Assert.Equal(300.0, result);
        }

        #endregion

        #region Subtraction Tests
        [Fact]
        public void SubtractionTest()
        {
            int result = MathApp.MathOperations.Subtract(5, 3);
            Assert.Equal(2, result);
        }

        [Fact]
        public void SubtractionTest2()
        {
            int result = MathApp.MathOperations.Subtract(4, 6);
            Assert.Equal(-2, result);
        }

        [Fact]
        public void SubtractionTest3()
        {
            int result = MathApp.MathOperations.Subtract(9, 3);
            Assert.Equal(6, result);
        }
        #endregion


        #region Modulus Tests
        [Theory]
        [InlineData(10, 3, 1)]
        [InlineData(-10, 3, -1)]
        [InlineData(10, -3, 1)]
        [InlineData(-10, -3, -1)]
        [InlineData(5, 5, 0)]
        public void ComputesRemainderForValidDivisors(double dividend, double divisor, double expected)
        {
            double result = MathApp.MathOperations.Modulus(dividend, divisor);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ThrowsWhenDivisorIsZero()
        {
            Assert.Throws<ArgumentException>(() => MathApp.MathOperations.Modulus(10, 0));
        }
        #endregion
    }
}