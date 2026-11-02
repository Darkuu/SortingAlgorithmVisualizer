using System;
using System.Collections.Generic;

namespace SortingVisualizer;

public record SortStep(
    int[] Values,
    int First = -1,
    int Second = -1);

public static class SortingAlgorithms
{
    private static SortStep Snapshot(
        int[] values,
        int first = -1,
        int second = -1)
    {
        return new SortStep(
            (int[])values.Clone(),
            first,
            second);
    }

    private static void Swap(
        int[] values,
        int first,
        int second)
    {
        (values[first], values[second]) =
            (values[second], values[first]);
    }

    public static IEnumerable<SortStep> BubbleSort(int[] values)
    {
        for (int end = values.Length - 1; end > 0; end--)
        {
            bool swapped = false;

            for (int j = 0; j < end; j++)
            {
                yield return Snapshot(values, j, j + 1);

                if (values[j] > values[j + 1])
                {
                    Swap(values, j, j + 1);
                    swapped = true;

                    yield return Snapshot(values, j, j + 1);
                }
            }

            if (!swapped)
            {
                yield break;
            }
        }
    }

    public static IEnumerable<SortStep> SelectionSort(int[] values)
    {
        for (int i = 0; i < values.Length - 1; i++)
        {
            int minimum = i;

            for (int j = i + 1; j < values.Length; j++)
            {
                yield return Snapshot(values, minimum, j);

                if (values[j] < values[minimum])
                {
                    minimum = j;
                }
            }

            if (minimum != i)
            {
                Swap(values, i, minimum);

                yield return Snapshot(values, i, minimum);
            }
        }
    }

    public static IEnumerable<SortStep> InsertionSort(int[] values)
    {
        // Adjacent swaps make movement easy to visualize.
        for (int i = 1; i < values.Length; i++)
        {
            int j = i;

            while (j > 0)
            {
                yield return Snapshot(values, j - 1, j);

                if (values[j - 1] <= values[j])
                {
                    break;
                }

                Swap(values, j - 1, j);

                yield return Snapshot(values, j - 1, j);

                j--;
            }
        }
    }

    public static IEnumerable<SortStep> MergeSort(int[] values)
    {
        return MergeRange(values, 0, values.Length - 1);
    }

    private static IEnumerable<SortStep> MergeRange(
        int[] values,
        int left,
        int right)
    {
        if (left >= right)
        {
            yield break;
        }

        int middle = left + (right - left) / 2;

        foreach (SortStep step in MergeRange(values, left, middle))
        {
            yield return step;
        }

        foreach (SortStep step in MergeRange(
                     values, middle + 1, right))
        {
            yield return step;
        }

        int[] leftHalf = new int[middle - left + 1];
        int[] rightHalf = new int[right - middle];

        Array.Copy(
            values,
            left,
            leftHalf,
            0,
            leftHalf.Length);

        Array.Copy(
            values,
            middle + 1,
            rightHalf,
            0,
            rightHalf.Length);

        int i = 0;
        int j = 0;
        int destination = left;

        while (i < leftHalf.Length && j < rightHalf.Length)
        {
            // Highlight the original positions of the values
            // being compared in the temporary arrays.
            yield return Snapshot(
                values,
                left + i,
                middle + 1 + j);

            if (leftHalf[i] <= rightHalf[j])
            {
                values[destination] = leftHalf[i];
                i++;
            }
            else
            {
                values[destination] = rightHalf[j];
                j++;
            }

            yield return Snapshot(values, destination);

            destination++;
        }

        while (i < leftHalf.Length)
        {
            values[destination] = leftHalf[i];

            yield return Snapshot(values, destination);

            i++;
            destination++;
        }

        while (j < rightHalf.Length)
        {
            values[destination] = rightHalf[j];

            yield return Snapshot(values, destination);

            j++;
            destination++;
        }
    }

    public static IEnumerable<SortStep> QuickSort(int[] values)
    {
        return QuickRange(values, 0, values.Length - 1);
    }

    private static IEnumerable<SortStep> QuickRange(
        int[] values,
        int low,
        int high)
    {
        if (low >= high)
        {
            yield break;
        }

        // The last value is the pivot.
        int pivot = values[high];
        int boundary = low;

        for (int j = low; j < high; j++)
        {
            yield return Snapshot(values, j, high);

            if (values[j] < pivot)
            {
                if (boundary != j)
                {
                    Swap(values, boundary, j);

                    yield return Snapshot(values, boundary, j);
                }

                boundary++;
            }
        }

        Swap(values, boundary, high);

        yield return Snapshot(values, boundary, high);

        foreach (SortStep step in QuickRange(
                     values, low, boundary - 1))
        {
            yield return step;
        }

        foreach (SortStep step in QuickRange(
                     values, boundary + 1, high))
        {
            yield return step;
        }
    }
}