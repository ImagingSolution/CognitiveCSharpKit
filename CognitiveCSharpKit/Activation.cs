using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CNTK;

namespace CognitiveCSharpKit
{
    /// <summary>
    /// 活性化関数
    /// </summary>
    public static partial class Layers
    {
        /// <summary>
        /// 活性化関数(Sigmoid関数)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Function Sigmoid(this Function input, string name = "")
        {
            return CNTKLib.Sigmoid(input, name);
        }

        /// <summary>
        /// 活性化関数(ReLU)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Function ReLU(this Function input, string name = "")
        {
            return CNTKLib.ReLU(input, name);
        }

        /// <summary>
        /// 活性化関数(Tanh)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Function Tanh(this Function input, string name = "")
        {
            return CNTKLib.Tanh(input, name);
        }

        /// <summary>
        /// 活性化関数(Softmax)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Function Softmax(this Function input, string name = "")
        {
            return CNTKLib.Softmax(input, name);
        }
    }
}
