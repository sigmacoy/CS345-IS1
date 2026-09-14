// Mamdani Codes: 

namespace FuzzyLogic
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press 'a' for Mamdani, 'b' for Seguno:");
            char input = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (input == 'a')
            {
                Console.WriteLine("=== True Mamdani Fuzzy Logic Controller Demo (.NET Framework) ===");
                // Define crisp inputs
                double temperature = 32.5; // Celsius
                double humidity = 80.0;    // Percentage (%)
                Console.WriteLine($"\nCrisp Inputs -> Temperature: {temperature}°C, Humidity: {humidity}%");

                // 1. FUZZIFICATION (Input Memberships)
                double tempLow = TriangularMembership(temperature, 10, 15, 20);
                double tempMed = TriangularMembership(temperature, 15, 25, 35);
                double tempHigh = TriangularMembership(temperature, 30, 35, 40);

                double humLow = TriangularMembership(humidity, 0, 25, 50);
                double humHigh = TriangularMembership(humidity, 40, 75, 100);

                Console.WriteLine("\n--- Fuzzification Results ---");
                Console.WriteLine($"Temp [Low: {tempLow:F2}, Med: {tempMed:F2}, High: {tempHigh:F2}]");
                Console.WriteLine($"Humidity [Low: {humLow:F2}, High: {humHigh:F2}]");

                // 2. RULE EVALUATION (Firing Strengths)
                // Rule 1: IF Temp is High OR Humidity is High, THEN Fan is Fast
                // Rule 2: IF Temp is Medium AND Humidity is Low, THEN Fan is Medium
                // Rule 3: IF Temp is Low, THEN Fan is Slow
                double rule1_strength = Math.Max(tempHigh, humHigh); // Fast
                double rule2_strength = Math.Min(tempMed, humLow);   // Medium
                double rule3_strength = tempLow;                     // Slow

                Console.WriteLine("\n--- Rule Firing Strengths ---");
                Console.WriteLine($"Rule 1 (Fast Fan): {rule1_strength:F2}");
                Console.WriteLine($"Rule 2 (Medium Fan): {rule2_strength:F2}");
                Console.WriteLine($"Rule 3 (Slow Fan): {rule3_strength:F2}");

                // 3. IMPLICATION, AGGREGATION & DEFUZZIFICATION (Center of Gravity)
                // We evaluate the output universe of discourse (Fan Speed: 0% to 100%) 
                // across discrete integration steps to find the geometric centroid.
                double sumNumerator = 0.0;
                double sumDenominator = 0.0;
                double step = 0.5; // Integration step size for accuracy

                for (double y = 0.0; y <= 100.0; y += step)
                {
                    // Define output membership functions for Fan Speed (Slow, Medium, Fast)
                    double outSlow = TriangularMembership(y, 0.0, 0.0, 50.0);
                    double outMed = TriangularMembership(y, 20.0, 50.0, 80.0);
                    double outFast = TriangularMembership(y, 50.0, 100.0, 100.0);

                    // Implication: Clip each output fuzzy set by its rule firing strength using Min
                    double clippedSlow = Math.Min(rule3_strength, outSlow);
                    double clippedMed = Math.Min(rule2_strength, outMed);
                    double clippedFast = Math.Min(rule1_strength, outFast);

                    // Aggregation: Combine all clipped output sets using Max (Union operator)
                    double aggregatedY = Math.Max(clippedSlow, Math.Max(clippedMed, clippedFast));

                    // Accumulate for Center of Gravity (Centroid) calculation:
                    // Centroid = Integral(y * u(y)) / Integral(u(y))
                    sumNumerator += y * aggregatedY * step;
                    sumDenominator += aggregatedY * step;
                }

                double crispOutput = 0.0;
                if (sumDenominator > 0.0)
                {
                    crispOutput = sumNumerator / sumDenominator;
                }

                Console.WriteLine("\n--- Mamdani Defuzzification Result (Centroid) ---");
                Console.WriteLine($"Calculated Crisp Fan Speed Output: {crispOutput:F2}%");
            }
            else if (input == 'b')
            {
                Console.WriteLine("=== Fuzzy Logic Controller Demo (.NET Framework) ===");
                // Define crisp inputs
                double temperature = 32.5; // e.g., Celsius
                double humidity = 80.0;    // e.g., Percentage (%)
                Console.WriteLine($"\nCrisp Inputs -> Temperature: {temperature}°C, Humidity: {humidity}%");

                // 1. FUZZIFICATION
                // Temperature memberships: Low, Medium, High
                double tempLow = TriangularMembership(temperature, 10, 15, 20);
                double tempMed = TriangularMembership(temperature, 15, 25, 35);
                double tempHigh = TriangularMembership(temperature, 30, 35, 40);

                // Humidity memberships: Low, High
                double humLow = TriangularMembership(humidity, 0, 25, 50);
                double humHigh = TriangularMembership(humidity, 40, 75, 100);

                Console.WriteLine("\n--- Fuzzification Results ---");
                Console.WriteLine($"Temp [Low: {tempLow:F2}, Med: {tempMed:F2}, High: {tempHigh:F2}]");
                Console.WriteLine($"Humidity [Low: {humLow:F2}, High: {humHigh:F2}]");

                // 2. RULE EVALUATION (Mamdani-style min-inference)
                // Rule 1: IF Temp is High OR Humidity is High, THEN Fan is Fast
                // Rule 2: IF Temp is Medium AND Humidity is Low, THEN Fan is Medium
                // Rule 3: IF Temp is Low, THEN Fan is Slow
                double rule1_strength = Math.Max(tempHigh, humHigh); // OR operator -> Max
                double rule2_strength = Math.Min(tempMed, humLow);   // AND operator -> Min
                double rule3_strength = tempLow;

                Console.WriteLine("\n--- Rule Evaluation Strengths ---");
                Console.WriteLine($"Rule 1 (Fast Fan): {rule1_strength:F2}");
                Console.WriteLine($"Rule 2 (Medium Fan): {rule2_strength:F2}");
                Console.WriteLine($"Rule 3 (Slow Fan): {rule3_strength:F2}");

                // 3. DEFUZZIFICATION (Weighted Average / Centroid approximation)
                // Let output centers be: Slow = 20 RPM/Speed, Medium = 60, Fast = 100
                double cSlow = 20.0;
                double cMed = 60.0;
                double cFast = 100.0;

                double numerator = (rule3_strength * cSlow) + (rule2_strength * cMed) + (rule1_strength * cFast);
                double denominator = rule3_strength + rule2_strength + rule1_strength;

                double crispOutput = 0.0;
                if (denominator > 0)
                {
                    crispOutput = numerator / denominator;
                }

                Console.WriteLine("\n--- Defuzzification Result ---");
                Console.WriteLine($"Calculated Crisp Fan Speed Output: {crispOutput:F2}% (or RPM Scale)");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        // Helper function for Triangular Membership Function
        // Parameters: x (input value), a (left foot), b (peak), c (right foot)
        static double TriangularMembership(double x, double a, double b, double c)
        {
            if (x <= a || x >= c)
                return 0.0;
            if (x == b)
                return 1.0;
            if (x > a && x < b)
                return (x - a) / (b - a);
            return (c - x) / (c - b);
        }
    }
}