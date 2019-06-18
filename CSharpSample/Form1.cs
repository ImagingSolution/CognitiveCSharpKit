using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CognitiveCSharpKit;
using CNTK;

namespace CSharpSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnLogisticRegression_Click(object sender, EventArgs e)
        {
            // Chart領域の初期化
            Utilities.InitChart(chtTraining);

            //LogisticRegression.TrainAndEvaluate(DeviceDescriptor.CPUDevice, chtTraining);

            int dstWidth = 28;
            int dstHeight = 28;
            int srcChannel;

            // 学習用データの取得
            int numOfCategories;

            var trainingDataMap = Data.PrepareTrainingDataFromSubfolders(
                @"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\test",   // 画像データが格納されたフォルダ
                dstWidth, // リサイズ後の幅
                dstHeight,　// リサイズ後の高さ
                out srcChannel,
                out numOfCategories
                );

            /////////////////////////////////////////////////////////////////////////////
            // ニューラルネットワークモデルの構築

            // デバイス

            // 入力サイズ28x28
            var inputData = Layers.Input(dstWidth, dstHeight, srcChannel);

            // ネットワークの構築
            var model =
                inputData
                    .Dense(40)
                    .Sigmoid()
                    .Dense(numOfCategories)
                ;

            // ラベルデータの確保(出力と同じサイズ)
            var labelData = model.LabelData();

            // 誤差関数
            Function trainingLoss = CNTKLib.CrossEntropyWithSoftmax(model, labelData);
            //Function trainingLoss = CNTKLib.BinaryCrossEntropy(model, labelData);
            // 分類誤差
            Function predictionError = CNTKLib.ClassificationError(model, labelData);

            /////////////////////////////////////////////////////////////////////////////
            // 学習器の構築

            int numMinibatches = 10;// 5;
            float learningRatePerMinibatch = 0.2F;
            float learningmomentumPerMinibatch = 0.9F;
            float l2RegularizationWeight = 0.1F;

            // 学習パラメータの設定
            //AdditionalLearningOptions additionalLearningOptions =
            //    new AdditionalLearningOptions() { l2RegularizationWeight = l2RegularizationWeight };
            //IList<Learner> parameterLearners = new List<Learner>() {
            //        Learner.MomentumSGDLearner(model.Parameters(),
            //        new TrainingParameterScheduleDouble(learningRatePerMinibatch, 0),
            //        new TrainingParameterScheduleDouble(learningmomentumPerMinibatch, 0),
            //        true,
            //        additionalLearningOptions)};
            //// 学習器の作成
            //var trainer = Trainer.CreateTrainer(model, trainingLoss, predictionError, parameterLearners);

            // 学習率
            CNTK.TrainingParameterScheduleDouble learningRatePerSample = new CNTK.TrainingParameterScheduleDouble(0.02, 1);
            // 確率的勾配降下法（Stochastic Gradient Descent）の適応
            var list = model.Parameters();
            IList<Learner> parameterLearners =
                new List<Learner>() { Learner.SGDLearner(model.Parameters(), learningRatePerSample) };
            // 学習の構築
            var trainer = Trainer.CreateTrainer(model, trainingLoss, predictionError, parameterLearners);


            int updatePerMinibatches = numMinibatches / 10;
            if (updatePerMinibatches == 0) updatePerMinibatches = 1;

            // Chart領域の初期化
            Utilities.InitChart(chtTraining);

            // 学習
            for (int minibatchCount = 0; minibatchCount < numMinibatches; ++minibatchCount)
            {
                Value imageBatch, labelBatch;
                int batchCount = 0, batchSize = 10;// 15;
                while (Data.GetImageAndLabelMinibatch(trainingDataMap, batchSize, batchCount++,
                    dstWidth, dstHeight, srcChannel, numOfCategories, out imageBatch, out labelBatch))
                {
                    //TODO: sweepEnd should be set properly.
#pragma warning disable 618
                    trainer.TrainMinibatch(new Dictionary<Variable, Value>() {
                    { (Variable)inputData, imageBatch },
                        { (Variable)labelData, labelBatch } }, Layers.GetDevice());
#pragma warning restore 618
                    //PrintTrainingProgress(trainer, minibatchCount, 1);

                }
                // 学習過程の表示
                Utilities.DrawTrainingProgress(trainer, minibatchCount, updatePerMinibatches, chtTraining);
            }

            var arr = model.Save();
            //model.Save("SaveModeTest.model");

            Evaluate eva = new Evaluate(arr);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var val = eva.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\9\7.png");
            Console.WriteLine($"9, {val}");
            val = eva.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\9\125.png");
            Console.WriteLine($"4, {val}");
            val = eva.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\5\120.png");
            Console.WriteLine($"9, {val}");
            val = eva.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\8\233.png");
            Console.WriteLine($"4, {val}");

            sw.Stop();
            Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
        }
    }
}
