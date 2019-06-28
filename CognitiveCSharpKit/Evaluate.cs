using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CNTK;


namespace CognitiveCSharpKit
{
    public static partial class Layers  //public class Evaluate
    {
        //Function _function;
        //Variable _inputVar;


        public static Function LoadModel(string modelFilename)
        {
            return Function.Load(modelFilename, Layers._device);
        }

        public static Function LoadModel(byte[] modelBuf)
        {
            return Function.Load(modelBuf, Layers._device);
        }
/*
        public Evaluate(string modelFilename)
        {
            _function = Function.Load(modelFilename, Layers._device);

            _inputVar = _function.Arguments.Single();

        }

        public Evaluate(byte[] modelBuf)
        {
            _function = Function.Load(modelBuf, Layers._device);

            _inputVar = _function.Arguments.Single();
        }

        public int ImageWidth
        {
            get
            {
                return _inputVar.Shape[0];
            }
        }

        public int ImageHeight
        {
            get
            {
                return _inputVar.Shape[1];
            }
        }

        public int ImageChannel
        {
            get
            {
                return _inputVar.Shape[2];
            }
        }
*/
        public static float[] EvaluateImage(this Function function, string imageFilename)
        {
            int channel;

            Variable inputVar = function.Arguments.Single();

            int imageWidth = inputVar.Shape[0];
            int imageHeight = inputVar.Shape[1];

            float[] resizedCHW = Data.LoadBitmap(imageFilename, imageWidth, imageHeight, out channel);
            //byte[] resizedCHW = Data.LoadBitmapByte(imageFilename, imageWidth, imageHeight, out channel);


            var inputDataMap = new Dictionary<Variable, Value>();
            var inputVal = Value.CreateBatch(new int[] { imageWidth, imageHeight, channel }, resizedCHW, Layers._device);

            inputDataMap.Add(inputVar, inputVal);

            Variable outputVar = function.Output;

            var outputDataMap = new Dictionary<Variable, Value>();
            outputDataMap.Add(outputVar, null);

            function.Evaluate(inputDataMap, outputDataMap, Layers._device);

            // Get evaluate result as dense output
            var outputVal = outputDataMap[outputVar];
            var outputData = outputVal.GetDenseData<float>(outputVar);

            return outputData[0].ToArray();
        }

        public static float[] EvaluateImageByte(this Function function, string imageFilename)
        {
            int channel;

            Variable inputVar = function.Arguments.Single();

            int imageWidth = inputVar.Shape[0];
            int imageHeight = inputVar.Shape[1];

            float[] resizedCHW = Data.LoadBitmap(imageFilename, imageWidth, imageHeight, out channel);

            var inputDataMap = new Dictionary<Variable, Value>();
            var inputVal = Value.CreateBatch(new int[] { imageWidth, imageHeight, channel }, resizedCHW, Layers._device);
            inputDataMap.Add(inputVar, inputVal);

            Variable outputVar = function.Output;

            var outputDataMap = new Dictionary<Variable, Value>();
            outputDataMap.Add(outputVar, null);

            function.Evaluate(inputDataMap, outputDataMap, Layers._device);

            // Get evaluate result as dense output
            var outputVal = outputDataMap[outputVar];
            var outputData = outputVal.GetDenseData<float>(outputVar);

            return outputData[0].ToArray();
        }


    }
}
