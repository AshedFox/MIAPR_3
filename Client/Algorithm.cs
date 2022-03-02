using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Client;

public static class Algorithm
{
    public static double GenerateRandomDouble(int min, int max) => RandomNumberGenerator.GetInt32(min, max);
    
    public static double CountMathExpectation(ICollection<double> values) => values.Sum() / values.Count;

    public static double CountStandardDeviation(double mathExpectation, ICollection<double> values) =>
        Math.Sqrt(values.Sum(v => Math.Pow(v - mathExpectation, 2)) / values.Count);

    public static double CountProbabilityDensity(double mathExpectation, double standardDeviation, double value) =>
        Math.Exp(-0.5 * Math.Pow((value - mathExpectation) / standardDeviation, 2)) /
        (standardDeviation * Math.Sqrt(2 * Math.PI));

    public static (double error, double end) CountFalseAlertError(double start, double mathExpectation1, double mathExpectation2,
        double standardDeviation1, double standardDeviation2, double probability1, double probability2)

    {
        double result = 0;
        var x = start;

        double g1, g2;

        do
        {
            g1 = CountProbabilityDensity(mathExpectation1, standardDeviation1, x) * probability1;
            g2 = CountProbabilityDensity(mathExpectation2, standardDeviation2, x) * probability2;
            result += g2 * 0.001;
            x += 0.001;
        } while (g2 < g1);

        return (error: result, end: x);
    }

    public static double CountMissingDetectionError(double start, double end, double errStart, double mathExpectation1, 
        double standardDeviation1, double probability1)
    {
        var x = start;
        var result = errStart;

        while (x < end)
        {
            var g1 = CountProbabilityDensity(mathExpectation1, standardDeviation1, x) * probability1;
            
            result += g1 * 0.001;
            x += 0.001;
        }

        return result;
    }
}