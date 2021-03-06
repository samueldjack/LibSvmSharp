﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibSvm
{
  abstract class Kernel : QMatrix
  {
    private readonly SvmNode[][] x;
    private readonly double[] x_square;

    // svm_parameter
    private readonly KernelType kernel_type;
    private readonly int degree;
    private readonly double gamma;
    private readonly double coef0;

    public override void SwapIndex(int i, int j)
    {
      Common.Swap(ref x[i], ref x[j]);
      if (x_square != null)
      {
        Common.Swap(ref x_square[i], ref x_square[j]);
      }
    }

    private static double powi(double base_, int times)
    {
      double tmp = base_, ret = 1.0;

      for (int t = times; t > 0; t /= 2)
      {
        if (t % 2 == 1) ret *= tmp;
        tmp = tmp * tmp;
      }
      return ret;
    }

    protected double kernel_function(int i, int j)
    {
      switch (kernel_type)
      {
        case KernelType.Linear:
          return dot(x[i], x[j]);
        case KernelType.Poly:
          return powi(gamma * dot(x[i], x[j]) + coef0, degree);
        case KernelType.Rbf:
          return Math.Exp(-gamma * (x_square[i] + x_square[j] - 2 * dot(x[i], x[j])));
        case KernelType.Sigmoid:
          return Math.Tanh(gamma * dot(x[i], x[j]) + coef0);
        case KernelType.Precomputed:
          return x[i][(int)(x[j][0].Value)].Value;
        default:
          throw new ApplicationException("Bad kernel_type");
      }
    }

    protected Kernel(int l, SvmNode[][] x_, SvmParameter param)
    {
      this.kernel_type = param.KernelType;
      this.degree = param.Degree;
      this.gamma = param.Gamma;
      this.coef0 = param.Coef0;

      x = (SvmNode[][])x_.Clone();

      if (kernel_type == KernelType.Rbf)
      {
        x_square = new double[l];
        for (int i = 0; i < l; i++)
          x_square[i] = dot(x[i], x[i]);
      }
      else x_square = null;
    }

    static double dot(SvmNode[] x, SvmNode[] y)
    {
      double sum = 0;
      int xlen = x.Length;
      int ylen = y.Length;
      int i = 0;
      int j = 0;
      while (i < xlen && j < ylen)
      {
        if (x[i].Index == y[j].Index)
        {
          sum += x[i++].Value * y[j++].Value;
        }
        else
        {
          if (x[i].Index > y[j].Index)
          {
            ++j;
          }
          else
          {
            ++i;
          }
        }
      }
      return sum;
    }

    public static double k_function(SvmNode[] x, SvmNode[] y, SvmParameter param)
    {
      switch (param.KernelType)
      {
        case KernelType.Linear:
          return dot(x, y);
        case KernelType.Poly:
          return powi(param.Gamma*dot(x, y) + param.Coef0, param.Degree);
        case KernelType.Rbf:
          double sum = 0;
          int xlen = x.Length;
          int ylen = y.Length;
          int i = 0;
          int j = 0;
          while (i < xlen && j < ylen)
          {
            if (x[i].Index == y[j].Index)
            {
              double d = x[i++].Value - y[j++].Value;
              sum += d * d;
            }
            else
            {
              if (x[i].Index > y[j].Index)
              {
                sum += y[j].Value*y[j].Value;
                ++j;
              }
              else
              {
                sum += x[i].Value*x[i].Value;
                ++i;
              }
            }
          }

          while (i < xlen)
          {
            sum += x[i].Value*x[i].Value;
            ++i;
          }

          while (j < ylen)
          {
            sum += y[j].Value*y[j].Value;
            ++j;
          }

          return Math.Exp(-param.Gamma*sum);
        case KernelType.Sigmoid:
          return Math.Tanh(param.Gamma*dot(x, y) + param.Coef0);
        case KernelType.Precomputed:
          return x[(int) (y[0].Value)].Value;
        default:
          throw new ApplicationException("Bad kernel_type");
      }
    }
  }

}
