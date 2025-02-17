﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS
{
    public class CoreDefine
    {
        public const float Epsilon = 0.00001f;
        public const float DEpsilon = 0.00001f;
        public const float PI = 3.14159f;
        public const float TWO_PI = PI * 2.0f;
        #region math
        public static float Sqrt(float v)
        {
            return (float)Math.Sqrt(v);
        }
        public static double Sqrt(double v)
        {
            return Math.Sqrt(v);
        }
        public static int Abs(int v)
        {
            return Math.Abs(v);
        }
        public static float Abs(float v)
        {
            return Math.Abs(v);
        }
        public static double Abs(double v)
        {
            return Math.Abs(v);
        }
        public static float Mod(float v, float d)
        {
            return v % d;
        }
        public static double Mod(double v, double d)
        {
            return v % d;
        }
        public static int Max(int left, int right)
        {
            return (left > right) ? left : right;
        }
        public static uint Max(uint left, uint right)
        {
            return (left > right) ? left : right;
        }
        public static float Max(float left, float right)
        {
            return (left > right) ? left : right;
        }
        public static int Min(int left, int right)
        {
            return (left < right) ? left : right;
        }
        public static uint Min(uint left, uint right)
        {
            return (left < right) ? left : right;
        }       
        public static float Min(float left, float right)
        {
            return (left < right) ? left : right;
        }
        public static float Angle_To_Tadian(float degree, float min, float second)
        {
            float flag = (degree < 0) ? -1.0f : 1.0f;
            if (degree < 0)
            {
                degree = degree * (-1.0f);
            }
            float angle = degree + min / 60 + second / 3600;
            float result = flag * (angle * PI) / 180;
            return result;
        }
        public static Vector3 Radian_To_Angle(float rad)
        {
            float flag = (rad < 0) ? -1.0f : 1.0f;
            if (rad < 0)
            {
                rad = rad * (-1.0f);
            }
            float result = (rad * 180) / PI;
            float degree = (float)((int)result);
            float min = (result - degree) * 60;
            float second = (min - (int)(min)) * 60;
            Vector3 ang;
            ang.X = flag * degree;
            ang.Y = (int)(min);
            ang.Z = second;
            return ang;
        }
        public static bool FloatEuqal(float f1, float f2, float epsilon)
        {
            return Math.Abs(f2 - f1) < epsilon;
        }
        public static bool DoubleEuqal(double f1, double f2, double epsilon)
        {
            return Math.Abs(f2 - f1) < epsilon;
        }
        public static float Lerp(float f1, float f2, float lp)
        {
            return f1 + (f2 - f1) * lp;
            //return f1 * lp + (1.0f - lp) * f2;
        }
        public static double Lerp(double f1, double f2, double lp)
        {
            return f1 + (f2 - f1) * lp;
            //return f1 * lp + (1.0 - lp) * f2;
        }
        public static int Clamp(int a, int min, int max)
        {
            if (a > max)
                return max;
            else if (a < min)
                return min;
            return a;
        }
        public static float Clamp(float a, float min, float max)
        {
            if (a > max)
                return max;
            else if (a < min)
                return min;
            return a;
        }
        public static uint Roundup(uint a, uint b)
        {
            uint result = a / b;
            if (a % b != 0)
                result += 1;
            return result;
        }
        public static int RoundUpPow2(int numToRound, int multiple)
        {
            System.Diagnostics.Debug.Assert(multiple!=0 && ((multiple & (multiple - 1)) == 0));
            return (numToRound + multiple - 1) & -multiple;
        }
        public static void Swap<T>(ref T lh, ref T rh)
        {
            T tmp = lh;
            lh = rh;
            rh = tmp;
        }
        public static float Floor(float v)
        {
            return (float)Math.Floor(v);
        }
        public static double Floor(double v)
        {
            return Math.Floor(v);
        }
        public static int FloorToInt(float v)
        {
            return (int)Math.Floor(v);
        }
        public static int FloorToInt(double v)
        {
            return (int)Math.Floor(v);
        }
        #endregion
    }
}
