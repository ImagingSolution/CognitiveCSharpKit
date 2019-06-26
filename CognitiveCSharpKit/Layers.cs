using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CNTK;

namespace CognitiveCSharpKit
{
    public static partial class Layers
    {

        // CPU or GPU
        internal static DeviceDescriptor _device = CNTK.DeviceDescriptor.CPUDevice;

        internal static CNTK.DataType _dataType = DataType.Float;

        /// <summary>
        /// CPU or GPU の自動選択（GPU優先）
        /// </summary>
        public static CNTK.DeviceDescriptor GetDevice()
        {
            foreach (var gpuDevice in CNTK.DeviceDescriptor.AllDevices())
            {
                if (gpuDevice.Type == CNTK.DeviceKind.GPU)
                {
                    _device = gpuDevice;
                    return _device;
                }
            }
            _device = CNTK.DeviceDescriptor.CPUDevice;

            return _device;
        }



        /// <summary>
        /// ニューラルネットワークモデルの入力設定
        /// </summary>
        /// <param name="width">入力画像の幅</param>
        /// <param name="height">入力画像の高さ</param>
        /// <param name="channel">入力画像のチャンネル</param>
        /// <param name="dataType">データタイプ</param>
        /// <param name="device">CPU/GPUの使用設定</param>
        /// <returns></returns>
        public static Function Input(int width, int height, int channel, CNTK.DataType dataType = DataType.Float, CNTK.DeviceDescriptor device = null, string name = "")
        {
            if (device == null)
            {
                // CPU or GPUの設定（GPU優先）
                GetDevice();
            }
            else
            {
                _device = device;
            }

            _dataType = dataType;

            return CognitiveCSharpKit.Data.Variable(width, height, channel, dataType, name);
        }

        public static Variable LabelData(this Function function, CNTK.DataType dataType = DataType.Float)
        {
            return CNTKLib.InputVariable(function.Output.Shape, dataType);
        }

        /// <summary>
        /// 全結合
        /// </summary>
        /// <param name="input"></param>
        /// <param name="outputDim"></param>
        /// <param name="withBias"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static Function Dense(this Function function, int outputDim, bool withBias = true, string name = "")
        {
            Variable input = (Variable)function;

            if (input.Shape.Rank != 1)
            {
                // 一次元でないとき、一次元に並び替える
                int newDim = input.Shape.Dimensions.Aggregate((d1, d2) => d1 * d2);
                input = CNTKLib.Reshape(input, new int[] { newDim });
            }

            var weightParam = new CNTK.Parameter(
                new int[] { outputDim, input.Shape.TotalSize },
                CNTK.DataType.Float,
                CNTK.CNTKLib.GlorotUniformInitializer(
                    CNTK.CNTKLib.DefaultParamInitScale,
                    CNTK.CNTKLib.SentinelValueForInferParamInitRank,
                    CNTK.CNTKLib.SentinelValueForInferParamInitRank,
                    1),
                //CNTK.CNTKLib.XavierInitializer(),
                _device,
                name + "_w"
                );

            // input * weightParam + biasParam
            if (withBias == true)
            {
                var biasParam = new Parameter(new int[] { outputDim }, DataType.Float, 0, _device, name + "_b");
                return CNTKLib.Plus(CNTKLib.Times(weightParam, input, name + "_times"), biasParam, name);
            }
            else
            {
                return CNTKLib.Times(weightParam, input, name);
            }
        }

        public static Function Scale(this Function function, float scale, string name = "")
        {

            return CNTKLib.ElementTimes(Constant.Scalar<float>(scale, _device), function);
        }


        /// <summary>
        /// コンボリューション
        /// </summary>
        /// <param name="input"></param>
        /// <param name="kernelWidth"></param>
        /// <param name="kernelHeight"></param>
        /// <param name="outFeatureMapCount"></param>
        /// <returns></returns>
        public static Function Convolution(
            this Function function,
            int kernelWidth,
            int kernelHeight,
            int outFeatureMapCount,
            int strideX = 1,
            int strideY = 1
            )
        {
            Variable input = (Variable)function;

            int numInputChannels = input.Shape.Dimensions[2];

            // parameter initialization hyper parameter
            double convWScale = 0.26;

            var convParams = new Parameter(
                new int[] { kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount },
                DataType.Float,
                CNTKLib.GlorotUniformInitializer(convWScale, -1, 2),
                _device
                );

            return CNTKLib.Convolution(convParams, input, new int[] { strideX, strideY, numInputChannels } /* strides */);
        }

        /// <summary>
        /// Max pooling
        /// </summary>
        /// <param name="input"></param>
        /// <param name="poolingWindowWidth"></param>
        /// <param name="poolingWindowHeight"></param>
        /// <param name="hStride"></param>
        /// <param name="vStride"></param>
        /// <returns></returns>
        public static Function MaxPooling(this Function function, int poolingWindowWidth, int poolingWindowHeight, int hStride, int vStride, bool padding)
        {
            return CNTKLib.Pooling(
                function,
                PoolingType.Max,
                new int[] { poolingWindowWidth, poolingWindowHeight },
                new int[] { hStride, vStride },
                new bool[] { padding }
                );
        }

        /// <summary>
        /// Average pooling
        /// </summary>
        /// <param name="input"></param>
        /// <param name="poolingWindowWidth"></param>
        /// <param name="poolingWindowHeight"></param>
        /// <param name="hStride"></param>
        /// <param name="vStride"></param>
        /// <returns></returns>
        public static Function AveragePooling(this Function function, int poolingWindowWidth, int poolingWindowHeight, int hStride, int vStride, bool padding)
        {
            return CNTKLib.Pooling(
                function,
                PoolingType.Average,
                new int[] { poolingWindowWidth, poolingWindowHeight },
                new int[] { hStride, vStride },
                new bool[] { padding }
                );
        }

        public static Function BatchNormalization(this Function function, float sigmaScale, float offset, int timeConst = 0, bool spatial = true, string name = "")
        {
            var biasParams = new Parameter(new int[] { NDShape.InferredDimension }, offset, _device, name + "_bias");
            var scaleParams = new Parameter(new int[] { NDShape.InferredDimension }, sigmaScale, _device, name + "_sigmaScale");
            var runningMean = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, _device);
            var runningInvStd = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, _device);
            var runningCount = Constant.Scalar(0.0f, _device);

            return CNTKLib.BatchNormalization(function, scaleParams, biasParams, runningMean, runningInvStd, runningCount,
                spatial, (double)timeConst, 0.0, 1e-5 /* epsilon */);
        }






    }
}
