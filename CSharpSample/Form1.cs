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

            /////////////////////////////////////////////////////////////////////////////
            // 学習用画像

            int dstWidth = 28;
            int dstHeight = 28;
            int srcChannel;

            // 学習用データの取得
            int numOfCategories;
            string[] categories;

            // 学習用データセット　List<Tuple<string, int, float[]>>　<ファイル名、カテゴリIndex 0, 1, 2...、画像データ配列 CHW順の一次元>
            var trainingDataMap = Data.PrepareTrainingDataFromSubfolders(
                @"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\test",   // 画像データが格納されたフォルダ
                dstWidth,       // リサイズ後の幅
                dstHeight,　    // リサイズ後の高さ
                out srcChannel, // 画像のチャンネル数
                out categories  // 指定フォルダ内にカテゴリ別に分けられたフォルダ名の配列
                );

            // クラス数
            numOfCategories = categories.Length;

            /////////////////////////////////////////////////////////////////////////////
            // ニューラルネットワークモデルの構築

            // 入力サイズ28x28
            var inputData = Layers.Input(dstWidth, dstHeight, srcChannel, DataType.Float);

            // ネットワークの構築
            var model =
                inputData
                    //.Scale(0.003125f)   // 1/256
                    //.BatchNormalization(0.5f, 0f)   // ±２σが±１になるぐらいを想定

                    .Convolution(
                        3, 3,   // kernelSize 幅、高さ
                        4,      // kernel数
                        1, 1    // stride　横、縦
                        )
                    .ReLU()
                    .MaxPooling(
                        3, 3,   // Poolingのサイズ 幅、高さ
                        2, 2,   // stride 横、縦
                        true    // paddingするか？
                        )

                    .Convolution(
                        3, 3,   // kernelSize 幅、高さ
                        8,      // kernel数
                        1, 1    // stride　横、縦
                        )
                    .ReLU()
                    .MaxPooling(
                        3, 3,   // Poolingのサイズ 幅、高さ
                        2, 2,   // stride　横、縦 
                        true    // paddingするか？
                        )

                    .Dense(numOfCategories)
                ;

            /////////////////////////////////////////////////////////////////////////////
            
            // ラベルデータの確保(出力と同じサイズ)
            var labelData = model.LabelData();
            // 損失関数
            Function trainingLoss = CNTKLib.CrossEntropyWithSoftmax(model, labelData);  // Softmax → CrossEntropy
            //Function trainingLoss = CNTKLib.BinaryCrossEntropy(model, labelData); // 出力が１つの場合
            // 分類誤差
            Function predictionError = CNTKLib.ClassificationError(model, labelData);

            /////////////////////////////////////////////////////////////////////////////
            // 学習

            int numMinibatches = 10;

            // 学習率
            CNTK.TrainingParameterScheduleDouble learningRatePerSample = new CNTK.TrainingParameterScheduleDouble(0.0002, 1); //(0.003125, 1);//(0.02, 1);
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

            ////////////////////////////////////////////////////////
            // 学習結果の保存、読み出し
            // 本プログラム的には意味はないが、学習と推論を切り分けるためのサンプル

            var modelFilename = "cntk_cnn.model";
            // モデルの保存
            model.Save(modelFilename);

            // モデルの読み出し
            // Softmax()は学習時のモデルには含まれていないため追加（CrossEntropyに含まれているため）
            Function loadModel = Layers.LoadModel(modelFilename).Softmax();

            ////////////////////////////////////////////////////////
            // 画像データの評価(推論)

            var val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\2\186.png");
            Console.WriteLine($"{categories[0]}:{val[0]}, {categories[1]}:{val[1]}, {categories[2]}:{val[2]}, {categories[3]}:{val[3]}");
            val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\4\109.png");
            Console.WriteLine($"{categories[0]}:{val[0]}, {categories[1]}:{val[1]}, {categories[2]}:{val[2]}, {categories[3]}:{val[3]}");
            val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\8\299.png");
            Console.WriteLine($"{categories[0]}:{val[0]}, {categories[1]}:{val[1]}, {categories[2]}:{val[2]}, {categories[3]}:{val[3]}");
            val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\9\118.png");
            Console.WriteLine($"{categories[0]}:{val[0]}, {categories[1]}:{val[1]}, {categories[2]}:{val[2]}, {categories[3]}:{val[3]}");

        }

        private void btnEvaluation_Click(object sender, EventArgs e)
        {
            var modelFilename = "cntk_cnn.model";
            // モデルの読み出し
            // Softmax()は学習時のモデルには含まれていないため追加（CrossEntropyに含まれているため）
            Function loadModel = Layers.LoadModel(modelFilename).Softmax();

            ////////////////////////////////////////////////////////
            // 画像データの評価(推論)

            var val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\2\186.png");
            Console.WriteLine($"{ val[0]}, { val[1]}, { val[2]}, { val[3]}");
            val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\4\109.png");
            Console.WriteLine($"{ val[0]}, { val[1]}, { val[2]}, { val[3]}");
            val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\8\299.png");
            Console.WriteLine($"{ val[0]}, { val[1]}, { val[2]}, { val[3]}");
            val = loadModel.EvaluateImage(@"D:\NeuralNetworkConsole\neural_network_console_140\samples\sample_dataset\MNIST\validation\9\118.png");
            Console.WriteLine($"{ val[0]}, { val[1]}, { val[2]}, { val[3]}");

        }
    }
}
