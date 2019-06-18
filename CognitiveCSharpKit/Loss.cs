using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CNTK;

namespace CognitiveCSharpKit
{
    public class Loss
    {

        public static Trainer BinaryCrossEntropy(Function input, Variable labelData, double learningRate, uint minibatchSize = 1)
        {
            var loss = CNTKLib.BinaryCrossEntropy(input, labelData);                        // ロス関数

            return PrepareTraining(input, labelData, loss, learningRate, minibatchSize);
        }

        public static Trainer SoftmaxCrossEntropy(Function input, Variable labelData, double learningRate, uint minibatchSize = 1)
        {
            var loss = CNTKLib.CrossEntropyWithSoftmax(input, labelData);                        // ロス関数(誤差関数？)

            return PrepareTraining(input, labelData, loss, learningRate, minibatchSize);
        }

        private static Trainer PrepareTraining(Function input, Variable labelData, Function loss, double learningRate, uint minibatchSize)
        {
            var evalError = CNTKLib.ClassificationError(input, labelData);                       // エラー関数（分類精度？）

            CNTK.TrainingParameterScheduleDouble learningRatePerSample = new CNTK.TrainingParameterScheduleDouble(learningRate, minibatchSize);
            IList<Learner> parameterLearners =
                new List<Learner>() { Learner.SGDLearner(input.Parameters(), learningRatePerSample) };

            return Trainer.CreateTrainer(input, loss, evalError, parameterLearners);
        }
    }
}
