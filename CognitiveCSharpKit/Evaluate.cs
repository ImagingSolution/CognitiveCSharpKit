using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CNTK;


namespace CognitiveCSharpKit
{
    public class Evaluate
    {
        Function _function;
        Variable _inputVar;

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

        public float EvaluateImage(string imageFilename)
        {
            int channel;

            float[] resizedCHW = Data.LoadBitmap(imageFilename, this.ImageWidth, this.ImageHeight, out channel);

            var inputDataMap = new Dictionary<Variable, Value>();
            var inputVal = Value.CreateBatch(new int[] { this.ImageWidth, this.ImageHeight, channel }, resizedCHW, Layers._device);
            inputDataMap.Add(_inputVar, inputVal);

            Variable outputVar = _function.Output;

            var outputDataMap = new Dictionary<Variable, Value>();
            outputDataMap.Add(outputVar, null);

            _function.Evaluate(inputDataMap, outputDataMap, Layers._device);

            // Get evaluate result as dense output
            var outputVal = outputDataMap[outputVar];
            var outputData = outputVal.GetDenseData<float>(outputVar);

            return outputData[0][0];
        }

    }
}
