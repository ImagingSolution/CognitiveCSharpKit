using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using CNTK;

namespace CognitiveCSharpKit
{
    public class Utilities
    {
        // CPU or GPU
        internal static DeviceDescriptor _device = CNTK.DeviceDescriptor.CPUDevice;

        // データタイプ 
        internal static CNTK.DataType _dataType = CNTK.DataType.Float;




        /// <summary>
        /// CPU or GPU 使用の設定
        /// </summary>
        /// <param name="device"></param>
        public static void SetDevice(DeviceDescriptor device)
        {
            _device = device;
        }

        /// <summary>
        /// 設定されているデバイスの取得
        /// </summary>
        /// <returns></returns>
        public static DeviceDescriptor GetDevice()
        {
            return _device;
        }

        /// <summary>
        /// データタイプの設定
        /// </summary>
        /// <param name="dataType"></param>
        public static void SetDataType(CNTK.DataType dataType)
        {
            _dataType = dataType;
        }

        /// <summary>
        /// データタイプの取得
        /// </summary>
        /// <param name="dataType"></param>
        public static CNTK.DataType GetDataType()
        {
            return _dataType;
        }

        /// <summary>
        /// RichTextBoxに学習過程を表示
        /// </summary>
        /// <param name="trainer"></param>
        /// <param name="minibatchIdx"></param>
        /// <param name="outputFrequencyInMinibatches"></param>
        /// <param name="rtb"></param>
        public static void PrintTrainingProgress(Trainer trainer, int minibatchIdx, int outputFrequencyInMinibatches, RichTextBox rtb)
        {
            if ((minibatchIdx % outputFrequencyInMinibatches) == 0 && trainer.PreviousMinibatchSampleCount() != 0)
            {
                float trainLossValue = (float)trainer.PreviousMinibatchLossAverage();
                float evaluationValue = (float)trainer.PreviousMinibatchEvaluationAverage();
                rtb.AppendText($"Minibatch: {minibatchIdx} CrossEntropyLoss = {trainLossValue}, EvaluationCriterion = {evaluationValue}\n");
            }
        }

        /// <summary>
        /// Chartに学習過程を表示
        /// </summary>
        /// <param name="trainer"></param>
        /// <param name="minibatchIdx"></param>
        /// <param name="outputFrequencyInMinibatches"></param>
        /// <param name="chart"></param>
        public static void DrawTrainingProgress(Trainer trainer, int minibatchIdx, int outputFrequencyInMinibatches, Chart chart)
        {
            if ((minibatchIdx % outputFrequencyInMinibatches) == 0 && trainer.PreviousMinibatchSampleCount() != 0)
            {
                float trainLossValue = (float)trainer.PreviousMinibatchLossAverage();
                float evaluationValue = (float)trainer.PreviousMinibatchEvaluationAverage();

                chart.Series["Loss"].Points.AddXY(minibatchIdx, trainLossValue);
                chart.Series["Evaluation"].Points.AddXY(minibatchIdx, evaluationValue);

                chart.Update();
                //rtb.AppendText($"Minibatch: {minibatchIdx} CrossEntropyLoss = {trainLossValue}, EvaluationCriterion = {evaluationValue}\n");
            }
        }

        /// <summary>
        /// Chartコントロールの初期化
        /// </summary>
        /// <param name="chart"></param>
        public static void InitChart(Chart chart)
        {
            // /////////////////////////////////////////////////////
            // Chartコントロール内のグラフ、凡例、目盛り領域を削除
            chart.Series.Clear();
            chart.Legends.Clear();
            chart.ChartAreas.Clear();

            // /////////////////////////////////////////////////////
            // 目盛り領域の設定
            var ca = chart.ChartAreas.Add("Training");

            // X軸
            ca.AxisX.Title = "Iterations";  // タイトル
            ca.AxisX.Minimum = 0;           // 最小値
            // Y軸
            ca.AxisY.Minimum = 0;

            var loss = chart.Series.Add("Loss");
            var evaluation = chart.Series.Add("Evaluation");

            // ２軸目に表示
            evaluation.YAxisType = AxisType.Secondary;

            loss.ChartType = SeriesChartType.Line;
            evaluation.ChartType = SeriesChartType.Line;

            loss.BorderWidth = 3;
            evaluation.BorderWidth = 3;

        }





    }
}
