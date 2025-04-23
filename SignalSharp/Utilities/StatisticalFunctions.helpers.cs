using System.Numerics;
using System.Runtime.CompilerServices;

namespace SignalSharp.Utilities;

public static partial class StatisticalFunctions
{
    private const int InsertionSortThreshold = 15;

    private static T MedianImplementation<T>(ReadOnlySpan<T> values, bool useQuickSelect)
        where T : INumber<T>
    {
        if (values.IsEmpty)
        {
            throw new ArgumentException("Input span must not be empty.", nameof(values));
        }

        var valuesArray = values.ToArray();

        if (useQuickSelect)
        {
            // O(n) average case, O(n^2) worst case
            return QuickSelectMedian(valuesArray);
        }

        // O(n log n) guaranteed performance
        Array.Sort(valuesArray);

        return CalculateMedianSorted(valuesArray);
    }

    private static T CalculateMedianSorted<T>(T[] sortedValues)
        where T : INumber<T>
    {
        int n = sortedValues.Length;
        int middle = n / 2;

        if (n % 2 != 0)
        {
            return sortedValues[middle];
        }

        T two = T.CreateChecked(2.0);
        return (sortedValues[middle - 1] + sortedValues[middle]) / two;
    }

    private static T QuickSelectMedian<T>(T[] values)
        where T : INumber<T>
    {
        int n = values.Length;
        int midIndex = n / 2;

        if (n % 2 != 0)
        {
            return QuickSelect(values, 0, n - 1, midIndex);
        }

        T midVal2 = QuickSelect(values, 0, n - 1, midIndex);
        T midVal1 = FindMaximum(values, 0, midIndex);
        T two = T.CreateChecked(2.0);
        return (midVal1 + midVal2) / two;
    }

    private static T FindMaximum<T>(T[] values, int start, int endExclusive)
        where T : INumber<T>
    {
        if (start >= endExclusive)
        {
            if (start == endExclusive - 1 && start >= 0 && start < values.Length)
                return values[start];

            throw new ArgumentException($"Invalid range for FindMax: start={start}, endExclusive={endExclusive}");
        }

        T maxVal = values[start];
        for (int i = start + 1; i < endExclusive; i++)
        {
            if (values[i] > maxVal)
            {
                maxVal = values[i];
            }
        }

        return maxVal;
    }

    private static T QuickSelect<T>(T[] values, int left, int right, int k)
        where T : INumber<T>
    {
        while (true)
        {
            if (left == right)
                return values[left];

            if (right - left + 1 <= InsertionSortThreshold)
            {
                InsertionSort(values, left, right);
                return values[k];
            }

            int pivotIndex = Partition(values, left, right);

            if (k == pivotIndex)
                return values[k];

            if (k < pivotIndex)
                right = pivotIndex - 1;
            else
                left = pivotIndex + 1;
        }
    }

    private static int Partition<T>(T[] values, int left, int right)
        where T : INumber<T>
    {
        int mid = left + (right - left) / 2;

        if (values[mid] < values[left])
            Swap(values, left, mid);
        if (values[right] < values[left])
            Swap(values, left, right);
        if (values[right] < values[mid])
            Swap(values, mid, right);

        Swap(values, mid, left);
        T pivot = values[left];

        int i = left;
        int j = right + 1;

        while (true)
        {
            do
            {
                i++;
            } while (i <= right && values[i] < pivot);

            do
            {
                j--;
            } while (j >= left && values[j] > pivot);

            if (i >= j)
                break;

            Swap(values, i, j);
        }

        Swap(values, left, j);
        return j;
    }

    private static void InsertionSort<T>(T[] values, int left, int right)
        where T : INumber<T>
    {
        for (int i = left + 1; i <= right; i++)
        {
            T key = values[i];
            int j = i - 1;
            while (j >= left && values[j] > key)
            {
                values[j + 1] = values[j];
                j--;
            }

            values[j + 1] = key;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap<T>(T[] values, int a, int b)
    {
        if (a == b)
            return;
        (values[a], values[b]) = (values[b], values[a]);
    }
}
