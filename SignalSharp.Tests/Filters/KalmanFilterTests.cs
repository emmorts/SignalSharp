using SignalSharp.Filters.Kalman;
// ReSharper disable InconsistentNaming

namespace SignalSharp.Tests.Filters;

using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

[TestFixture]
public class KalmanFilterTests
{
    [Test]
    public void KalmanFilter_SimpleInput_ReturnsFilteredOutput()
    {
        var F = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var B = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var H = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var Q = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var R = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var x0 = Vector<double>.Build.DenseOfArray([0]);
        var P0 = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        
        var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);

        double[] measurements = [1, 2, 3, 4, 5];
        double[] expected = [0.5, 1, 1.5, 2, 2.5];

        var (result, _) = kalmanFilter.Filter(measurements);

        Assert.That(result, Is.EqualTo(expected).Within(1e-1));
    }

    [Test]
    public void KalmanFilter_NoiseFreeMeasurements_ReturnsSameOutput()
    {
        var F = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var B = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var H = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var Q = Matrix<double>.Build.DenseOfArray(new[,] {{1e-5}});
        var R = Matrix<double>.Build.DenseOfArray(new[,] {{1e-5}});
        var x0 = Vector<double>.Build.DenseOfArray([0]);
        var P0 = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});

        var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);
        
        double[] measurements = [2, 3, 4, 5, 6];
        double[] expected = [2, 2.67, 3.5, 4.43, 5.4];
        
        var (result, _) = kalmanFilter.Filter(measurements);

        Assert.That(result, Is.EqualTo(expected).Within(1e-2));
    }

    [Test]
    public void KalmanFilter_VaryingMeasurements_ReturnsFilteredOutput()
    {
        var F = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var B = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var H = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var Q = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var R = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var x0 = Vector<double>.Build.DenseOfArray([0]);
        var P0 = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});

        var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);
        
        double[] measurements = [2.0, 1.0, 4.0, 3.0, 5.0];
        double[] expected = [1.0, 1.0, 1.75, 2, 2.5];
        
        var (result, _) = kalmanFilter.Filter(measurements);

        Assert.That(result, Is.EqualTo(expected).Within(1e-1));
    }

    [Test]
    public void KalmanFilter_NoMeasurements_ReturnsEmptyOutput()
    {
        var F = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var B = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var H = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var Q = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var R = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var x0 = Vector<double>.Build.DenseOfArray([0]);
        var P0 = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});

        var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);
        
        double[] measurements = [];

        var (result, _) = kalmanFilter.Filter(measurements);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void KalmanFilter_ConstantMeasurements_ReturnsSameOutput()
    {
        var F = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var B = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var H = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var Q = Matrix<double>.Build.DenseOfArray(new[,] {{1e-5}});
        var R = Matrix<double>.Build.DenseOfArray(new[,] {{1e-5}});
        var x0 = Vector<double>.Build.DenseOfArray([0]);
        var P0 = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});

        var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);
        
        double[] measurements = [5, 5, 5, 5, 5];
        double[] expected = [5, 5, 5, 5, 5];

        var (result, _) = kalmanFilter.Filter(measurements);

        Assert.That(result, Is.EqualTo(expected).Within(1e-3));
    }

    [Test]
    public void KalmanFilter_IncreasingLinearMeasurements_ReturnsFilteredOutput()
    {
        var F = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var B = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var H = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var Q = Matrix<double>.Build.DenseOfArray(new[,] {{1e-5}});
        var R = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var x0 = Vector<double>.Build.DenseOfArray([0]);
        var P0 = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});

        var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);
        
        double[] measurements = [1, 2, 3, 4, 5];
        double[] expected = [0.5, 1, 1.5, 2, 2.5];

        var (result, _) = kalmanFilter.Filter(measurements);

        Assert.That(result, Is.EqualTo(expected).Within(1e-1));
    }

    [Test]
    public void KalmanFilter_NegativeMeasurements_ReturnsFilteredOutput()
    {
        var F = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var B = Matrix<double>.Build.DenseOfArray(new double[,] {{0}});
        var H = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var Q = Matrix<double>.Build.DenseOfArray(new[,] {{1e-5}});
        var R = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});
        var x0 = Vector<double>.Build.DenseOfArray([0]);
        var P0 = Matrix<double>.Build.DenseOfArray(new double[,] {{1}});

        var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);
        
        double[] measurements = [-1, -2, -3, -4, -5];
        double[] expected = [-0.5, -1, -1.5, -2, -2.5];
        
        var (result, _) = kalmanFilter.Filter(measurements);

        Assert.That(result, Is.EqualTo(expected).Within(1e-1));
    }
}
