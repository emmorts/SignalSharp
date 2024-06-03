using MathNet.Numerics.LinearAlgebra;
// ReSharper disable InconsistentNaming

namespace SignalSharp.Filters.Kalman;

/// <summary>
/// Implements the Kalman filter for estimating the state of a linear dynamic system from a series of noisy measurements.
/// </summary>
/// <remarks>
/// <para>
/// The Kalman filter is an optimal recursive algorithm for estimating the state of a linear dynamic system 
/// from a series of noisy measurements. It is widely used in control systems, navigation, and time series analysis.
/// </para>
/// <para>
/// This implementation supports a basic discrete-time Kalman filter where the system is modeled using the state 
/// transition matrix (F), control input matrix (B), observation matrix (H), process noise covariance matrix (Q), 
/// and measurement noise covariance matrix (R).
/// </para>
/// <para>
/// The Kalman filter performs two steps: prediction and update. During the prediction step, the filter estimates 
/// the state at the next time step. During the update step, the filter adjusts this prediction based on the new measurement.
/// </para>
/// </remarks>
public class KalmanFilter
{
    private readonly Matrix<double> _F;
    private readonly Matrix<double> _B;
    private readonly Matrix<double> _H;
    private readonly Matrix<double> _Q;
    private readonly Matrix<double> _R;
    private readonly Matrix<double> _I;
    private Vector<double> _x;
    private Matrix<double> _P;

    /// <summary>
    /// Initializes a new instance of the KalmanFilter class with the specified system parameters.
    /// </summary>
    /// <param name="F">The state transition matrix.</param>
    /// <param name="B">The control input matrix.</param>
    /// <param name="H">The observation matrix.</param>
    /// <param name="Q">The process noise covariance matrix.</param>
    /// <param name="R">The measurement noise covariance matrix.</param>
    /// <param name="x0">The initial state vector.</param>
    /// <param name="P0">The initial covariance matrix.</param>
    public KalmanFilter(
        Matrix<double> F,
        Matrix<double> B,
        Matrix<double> H,
        Matrix<double> Q,
        Matrix<double> R,
        Vector<double> x0,
        Matrix<double> P0)
    {
        _F = F;
        _B = B;
        _H = H;
        _Q = Q;
        _R = R;
        _x = x0.Clone();
        _P = P0.Clone();
        _I = Matrix<double>.Build.DenseIdentity(x0.Count);
    }

    /// <summary>
    /// Applies the Kalman filter to a series of measurements, estimating the state and covariance at each step.
    /// </summary>
    /// <param name="measurements">An array of measurement values.</param>
    /// <returns>
    /// A tuple containing two elements:
    /// <list type="bullet">
    /// <item>An array of the filtered state estimates corresponding to the measurements.</item>
    /// <item>A 2D array of the estimated covariances at each time step.</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method processes the input measurements one by one, performing the prediction and update steps of the Kalman filter. 
    /// For each measurement, the state estimate and covariance are updated and stored.
    /// </para>
    /// <example>
    /// For example, to apply the Kalman filter to an array of measurements:
    /// <code>
    /// double[] measurements = {1.0, 2.0, 3.0, 4.0};
    /// var kalmanFilter = new KalmanFilter(F, B, H, Q, R, x0, P0);
    /// var result = kalmanFilter.Filter(measurements);
    /// double[] filteredValues = result.FilteredValues;
    /// double[,] estimatedCovariances = result.EstimatedCovariances;
    /// </code>
    /// </example>
    /// </remarks>
    public (double[] FilteredValues, double[,] EstimatedCovariances) Filter(double[] measurements)
    {
        var n = _x.Count;
        var m = measurements.Length;

        var filteredValues = new double[m];
        var estimatedCovariances = new double[m, n];

        for (var i = 0; i < m; i++)
        {
            Predict();
            Update(measurements[i]);

            filteredValues[i] = _x[0];
            for (var j = 0; j < n; j++)
            {
                estimatedCovariances[i, j] = _P[j, j];
            }
        }

        return (filteredValues, estimatedCovariances);
    }
    
    /// <summary>
    /// Predicts the next state and covariance of the system.
    /// </summary>
    private void Predict()
    {
        var x = _F * _x;
        var y = _B * Vector<double>.Build.Dense(1, 0);
        var xy = x + y;
        _x = _F * _x + _B * Vector<double>.Build.Dense(1, 0); // Assuming control input u is 0
        _P = _F * _P * _F.Transpose() + _Q;
    }

    /// <summary>
    /// Updates the state and covariance of the system based on the given measurement.
    /// </summary>
    /// <param name="measurement">The new measurement value.</param>
    private void Update(double measurement)
    {
        var z = Vector<double>.Build.Dense(1, measurement);
        var y = z - _H * _x;
        var S = _H * _P * _H.Transpose() + _R;
        var K = _P * _H.Transpose() * S.Inverse();

        _x = _x + K * y;
        _P = (_I - K * _H) * _P;
    }
}