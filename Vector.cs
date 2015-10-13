using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input;

namespace WPUnlock
{
    /// <summary>
    /// From David Ledo: http://cpsc581.davidledo.com/tutorials/code/Vector.cs
    /// Code partially based on http://www.codeproject.com/Articles/17425/A-Vector-Type-for-C
    /// </summary>
    public class Vector
    {
        public static readonly Vector Origin = new Vector(0, 0);
        public static readonly Vector X_Axis = new Vector(1, 0);
        public static readonly Vector Y_Axis = new Vector(0, 1);

        #region Instance Variables

        public double X { get; set; }
        public double Y { get; set; }

        #endregion

        #region Properties

        public double Length
        {
            get
            {
                return Math.Sqrt(LengthSquared);
            }
        }

        public double LengthSquared
        {
            get
            {
                return SumComponentSqrs(this);
            }
        }

        #endregion

        #region Operator Overload

        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector
            (
                v1.X + v2.X,
                v1.Y + v2.Y
            );
        }

        public static Vector operator -(Vector v1, Vector v2)
        {
            return new Vector
            (
                v1.X - v2.X,
                v1.Y - v2.Y
            );

        }

        public static Vector operator -(Vector v1)
        {
            return new Vector
            (
                -v1.X,
                -v1.Y
            );
        }

        public static bool operator <(Vector v1, Vector v2)
        {
            return v1.Length < v2.Length;
        }

        public static bool operator <=(Vector v1, Vector v2)
        {
            return v1.Length <= v2.Length;
        }

        public static bool operator >(Vector v1, Vector v2)
        {
            return v1.Length > v2.Length;
        }

        public static bool operator >=(Vector v1, Vector v2)
        {
            return v1.Length >= v2.Length;
        }

        public static bool operator ==(Vector v1, Vector v2)
        {
            return
            (
                v1.X == v2.X &&
                v1.Y == v2.Y
            );
        }

        public static bool operator !=(Vector v1, Vector v2)
        {
            return !(v1 == v2);
        }

        public static Vector operator /(Vector v1, double s2)
        {
            return new Vector
            (
                v1.X / s2,
                v1.Y / s2
            );
        }

        public static Vector operator *(Vector v1, double s2)
        {
            return new Vector
            (
                v1.X * s2,
                v1.Y * s2
            );
        }

        public static Vector operator *(double s1, Vector v2)
        {
            return v2 * s1;
        }

        #endregion

        #region Operations

        public static double CrossProduct(Vector v1, Vector v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        public double CrossProduct(Vector other)
        {
            return CrossProduct(this, other);
        }

        public static double DotProduct(Vector v1, Vector v2)
        {
            return v1.X * v2.X + v1.Y + v2.Y;
        }

        public double DotProduct(Vector other)
        {
            return DotProduct(this, other);
        }

        public static bool IsUnitVector(Vector v1)
        {
            return v1.Length == 1;
        }

        public bool IsUnitVector()
        {
            return IsUnitVector(this);
        }

        public static Vector Normalize(Vector v1)
        {
            // Check for divide by zero errors
            if (v1.Length == 0)
            {
                throw new DivideByZeroException("Magnitude for the Vector is currently 0");
            }
            else
            {
                // find the inverse of the vectors magnitude
                double inverse = 1 / v1.Length;
                return
                (
                   new Vector
                   (
                    // multiply each component by the inverse of the magnitude
                      v1.X * inverse,
                      v1.Y * inverse
                   )
                );
            }
        }

        public static Vector Interpolate(Vector v1, Vector v2, double control)
        {
            if (control > 1 || control < 0)
            {
                // Error message includes information about the actual value of the 
                // argument
                throw new ArgumentOutOfRangeException
                (
                   "Control Point must be between 0 and 1"
                );
            }
            else
            {
                return
                (
                   new Vector
                   (
                       v1.X * (1 - control) + v2.X * control,
                       v1.Y * (1 - control) + v2.Y * control
                    )
                );
            }
        }

        public Vector Interpolate(Vector other, double control)
        {
            return Interpolate(this, other, control);
        }

        public static Double Abs(Vector v1)
        {
            return v1.Length;
        }

        public static double Angle(Vector v1, Vector v2)
        {
            Vector a = Normalize(v1);
            Vector b = Normalize(v2);
            double angle = (Math.Atan2(a.Y, a.X) - Math.Atan2(b.Y, b.X));
            if (angle < 0)
            {
                angle += Math.PI * 2;
            }
            return
             (
                Math.Acos
                (
                   Normalize(v1).DotProduct(Normalize(v2))
                )
             );
        }

        public static double AnglePI(Vector v1, Vector v2)
        {
            double angle = Angle2PI(v1, v2);
            if (angle > Math.PI)
            {
                angle = (2 * Math.PI) - angle;
            }
            return angle;
        }

        public static double Angle2PI(Vector v1, Vector v2)
        {
            Vector a = Normalize(v1);
            Vector b = Normalize(v2);
            double angle = (Math.Atan2(a.Y, a.X) - Math.Atan2(b.Y, b.X));
            if (angle < 0)
            {
                angle += Math.PI * 2;
            }
            return angle;
        }

        public double Angle(Vector other)
        {
            return Angle(this, other);
        }

        public static Vector Max(Vector v1, Vector v2)
        {
            if (v1 >= v2) { return v1; }
            return v2;
        }

        public Vector Max(Vector other)
        {
            return Max(this, other);
        }

        public static Vector Min(Vector v1, Vector v2)
        {
            if (v1 <= v2) { return v1; }
            return v2;
        }

        public Vector Min(Vector other)
        {
            return Min(this, other);
        }


        public static bool IsPerpendicular(Vector v1, Vector v2)
        {
            return v1.DotProduct(v2) == 0;
        }

        public bool IsPerpendicular(Vector other)
        {
            return IsPerpendicular(this, other);
        }

        public static double SumComponents(Vector v1)
        {
            return (v1.X + v1.Y);
        }

        public double SumComponents()
        {
            return SumComponents(this);
        }

        public static Vector PowComponents(Vector v1, double power)
        {
            return
            (
               new Vector
               (
                  Math.Pow(v1.X, power),
                  Math.Pow(v1.Y, power)
               )
            );
        }

        public static Vector SqrtComponents(Vector v1)
        {
            return
            (
               new Vector
               (
                  Math.Sqrt(v1.X),
                  Math.Sqrt(v1.Y)
               )
            );
        }

        public static Vector SqrComponents(Vector v1)
        {
            return
            (
               new Vector
               (
                   v1.X * v1.X,
                   v1.Y * v1.Y
               )
             );
        }


        public static double SumComponentSqrs(Vector v1)
        {
            Vector v2 = SqrComponents(v1);
            return v2.SumComponents();
        }

        public override int GetHashCode()
        {
            return
            (
               (int)((X + Y) % Int32.MaxValue)
            );
        }

        public override string ToString()
        {
            return "Vector:: X = " + this.X.ToString("0.000") + ", " + this.Y.ToString("0.000");
        }

        #endregion

        #region Constructor

        public Vector()
        {
            this.X = 0;
            this.Y = 0;
        }

        public Vector(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        #endregion


    }
}
