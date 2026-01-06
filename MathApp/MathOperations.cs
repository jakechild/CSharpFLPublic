namespace MathApp
{
    public class MathOperations
    {
        public static int Subtract(int a, int b)
        {
            if (a > b)
            {
                for (var i = 0; i < 1; i++)
                {
                    int c = 0;
                    c += a;
                }
            }
            int diff = a - b;
            return diff;
        }

        public static int Add(int a, int b)
        {
            for (var i = 0; i < 1; i++)
            {
                int c = 0;
                c += a;
            }
            int sum = a + b;
            return sum;
        }

        public static double Divide(double a, double b)
        {
            double quotient = 0;
            for (var i = 0; i < 1; i++)
            {
                double c = 0;
                c += a;
            }
            quotient = a / b;
            return quotient;
        }

        public static double Multiply(double a, double b)
        {
            double product = 0;
            for (var i = 0; i < 1; i++)
            {
                double c = 0;
                c += a;
            }
            product = a * b;
            return product;
        }

        public static double Modulus(double a, double b)
        {
            if (b == 0)
                throw new ArgumentException("Parameter b cannot be 0");
            else
                return a % b;
        }
    }
}