using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

using System.IO;

using CNTK;

namespace CognitiveCSharpKit
{
    /// <summary>
    /// ニューラルネットワークで扱うデータに関してのクラス
    /// </summary>
    public class Data
    {
        /// <summary>
        /// 入力データ
        /// </summary>
        /// <param name="dataCount"></param>
        /// <returns></returns>
        public static Variable Variable(int dataLength, CNTK.DataType dataType = DataType.Float)
        {
            return CNTKLib.InputVariable(new int[] { dataLength }, dataType);
        }

        public static Variable Variable(int width, int height, int channel, CNTK.DataType dataType = DataType.Float, string name = "")
        {
            return CNTKLib.InputVariable(new int[] { width, height, channel }, dataType, name);
        }

        public static Variable Variable(Variable var, CNTK.DataType dataType = DataType.Float)
        {
            return CNTKLib.InputVariable(var.Shape, dataType);
        }

        /// <summary>
        /// ビットマップファイルをリサイズし、データの並びをB,G,R,B,G,R...からBBBBB,GGGGG,RRRRRへ並び替える
        /// </summary>
        /// <param name="filename">ビットマップファイルのファイル名</param>
        /// <param name="dstWidth">リサイズ後の画像の幅</param>
        /// <param name="dstHeight">リサイズ後の画像の高さ</param>
        /// <param name="channel">チャンネル数</param>
        /// <param name="chwData">変換されたデータ</param>
        /// <param name="normalize">データを0～1へ変換するか？</param>
        /// <returns></returns>
        public static int ResizeBitmapExtractCHW(
            string filename,
            int dstWidth,
            int dstHeight,
            out int channel,
            ref float[] chwData,
            bool normalize = true
            )
        {

            using (var fs = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                // ビットマップデータの取得
                var bitmapFrame = System.Windows.Media.Imaging.BitmapFrame.Create(
                        fs,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.Default
                    );
                // 元画像のフォーマット
                System.Windows.Media.PixelFormat srcFormat = bitmapFrame.Format;

                // 拡大・縮小されたビットマップを作成する
                var scaledBitmapSource
                    = new System.Windows.Media.Imaging.TransformedBitmap(
                        bitmapFrame,
                        new System.Windows.Media.ScaleTransform(dstWidth / (double)bitmapFrame.Width, dstHeight / (double)bitmapFrame.Height)
                        );

                // リサイズ後画像のフォーマット
                System.Windows.Media.PixelFormat resizedFormat = scaledBitmapSource.Format;
            }


            channel = 1; // 仮
            return 0;

        }


        /// <summary>
        /// Bitmap画像をリサイズしてfloat配列([c, h, w]の配列順番)へ輝度値を代入する
        /// </summary>
        /// <param name="image">Bitmapクラスオブジェクト</param>
        /// <param name="dstWidth">リサイズ後の幅</param>
        /// <param name="dstHeight">リサイズ後の高さ</param>
        /// <param name="channel">画像データのチャンネル数</param>
        /// <param name="chwData">輝度値を格納する配列</param>
        /// <param name="normalize">輝度値を正規化（1/255）するか？</param>
        /// <returns></returns>
        public static int ResizeBitmapExtractCHW(
            Bitmap src,
            int dstWidth,
            int dstHeight,
            out int channel,
            ref float[] chwData,
            bool normalize = true
            )
        {
            channel = 0;

            if (src == null) return -1;

            int srcWidth = src.Width;
            int srcHeight = src.Height;
            var pf = src.PixelFormat;
            channel = Bitmap.GetPixelFormatSize(pf) / 8;
            int dstChannelStride = dstWidth * dstHeight;

            int resizeCh = channel;

            if (channel == 1)
            {
                pf = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
                resizeCh = 3;
            }
            else if (channel == 4) // Alpha成分は無視する
            {
                pf = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
                channel = 3;
                resizeCh = 3;
            }

            if (chwData == null)
            {
                chwData = new float[channel * dstChannelStride];
            }

            if (chwData.Length != channel * dstChannelStride)
            {
                chwData = new float[channel * dstChannelStride];
            }

            // リサイズ後のBitmapクラス 
            using (var bmpResizeColor = new Bitmap(dstWidth, dstHeight, pf))
            {
                var g = Graphics.FromImage(bmpResizeColor);

                // リサイズして描画
                g.DrawImage(
                    src,
                    new RectangleF(0, 0, dstWidth, dstHeight),
                    new RectangleF(-0.5f, -0.5f, srcWidth, srcHeight),
                    GraphicsUnit.Pixel
                    );

                // カラー画像のポインタへアクセス
                var bmpDataColor = bmpResizeColor.LockBits(
                        new Rectangle(0, 0, dstWidth, dstHeight),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        pf
                        );

                IntPtr ptr = bmpDataColor.Scan0;
                int colorStride = Math.Abs(bmpDataColor.Stride);
                int bytes = colorStride * bmpDataColor.Height;

                byte[] rgbValues = new byte[colorStride * bmpDataColor.Height];

                // 配列へコピー
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                float scale = 1.0f;

                if (normalize == true)
                {
                    scale /= 255f;
                }

                float[] fArr = chwData;

                int paraChannel = channel;

                Parallel.For(0, dstHeight, y =>
                {
                    for (int x = 0; x < dstWidth; x++)
                    {
                        for (int c = 0; c < paraChannel; c++)
                        {
                            fArr[c * dstChannelStride + y * dstWidth + x] = rgbValues[y * colorStride + x * resizeCh + c] * scale;
                        }
                    }
                }
                );
                bmpResizeColor.UnlockBits(bmpDataColor);
            }
            return 0;
        }

        /// <summary>
        /// 指定フォルダにクラス別に格納された画像ファイルを読み込む
        /// </summary>
        /// <param name="rootFolderOfClassifiedImages">クラスごとにフォルダに格納された画像ファイルのあるフォルダ</param>
        /// <param name="dstWidth">リサイズ後の画像の幅</param>
        /// <param name="dstHeight">リサイズ後の画像の高さ</param>
        /// <param name="channel">画像のチャンネル数</param>
        /// <param name="categories">指定フォルダ内のフォルダ名の配列</param>
        /// <returns>ファイルパス、ラベル番号、画像データのTupleのリスト</returns>
        public static List<Tuple<string, int, float[]>> PrepareTrainingDataFromSubfolders(
            string rootFolderOfClassifiedImages,
            int dstWidth,
            int dstHeight,
            out int channel,
            //out int numOfCategories
            out string[] categories
            )
        {
            channel = 0;

            // ファイルパス、ラベル番号、画像データのTupleのリスト
            List<Tuple<string, int, float[]>> dataMap = new List<Tuple<string, int, float[]>>();

            // 指定フォルダ内のフォルダ（カテゴリに分けられたフォルダ）一覧の取得
            categories = System.IO.Directory.GetDirectories(
                rootFolderOfClassifiedImages, "*", System.IO.SearchOption.TopDirectoryOnly);

            //numOfCategories = categories.Length;

            int categoryIndex = 0;
            foreach (var category in categories)
            {
                // フォルダ内の画像ファイル（bmp, jpg, png, tif）一覧の取得
                List<string> files = new List<string>();

                foreach (string exp in new string[] { "*.bmp", "*.jpg", "*.png", "*.tif" })
                {
                    files.AddRange(System.IO.Directory.GetFiles(category, exp));
                }

                if (files.Count == 0) continue;


                foreach (var file in files)
                //Parallel.ForEach(files, file =>
                {
                    float[] image = Data.LoadBitmap(file, dstWidth, dstHeight, out channel);
                    //float[] image = LoadBitmap(file, dstWidth, dstHeight);
                    dataMap.Add(new Tuple<string, int, float[]>(file, categoryIndex, image));
                }
                //);
                categoryIndex++;
            }

            // カテゴリの配列をフォルダ名のみに変更
            for (int i = 0; i < categories.Length; i++)
            {
                categories[i] = System.IO.Path.GetFileName(categories[i]);
            }

            return dataMap;
        }

        /// <summary>
        /// 画像ファイルを開き、リサイズしてfloat配列を返す
        /// </summary>
        /// <param name="filePath">画像ファイルのパス</param>
        /// <param name="dstWidth">リサイズ後の画像の幅</param>
        /// <param name="dstHeight">リサイズ後の画像の高さ</param>
        /// <returns>[channel, height, width]の順に並んだ輝度値配列</returns>
        public static float[] LoadBitmap(string filePath, int dstWidth, int dstHeight, out int channel)
        {
            float[] chwData = null;

            using (FileStream fs = File.OpenRead(filePath))
            using (Bitmap bmp = (Bitmap)Bitmap.FromStream(fs, false, false))
            {
                ResizeBitmapExtractCHW(bmp, dstWidth, dstHeight, out channel, ref chwData);
            }
            return chwData;
        }

        private static float[] LoadBitmap(string filePath, int dstWidth, int dstHeight)
        {
            int channel;
            return LoadBitmap(filePath, dstWidth, dstHeight, out channel);
        }

        public static Dictionary<CNTK.Variable, CNTK.Value> SetVariableValue(CNTK.Variable variable, float[] value)
        {
            CNTK.Value inputValue = null;

            if (value != null)
            {
                inputValue = CNTK.Value.CreateBatch<float>(new int[] { variable.Shape.Dimensions[0] }, value, 0, value.Length, Layers._device);
            }
            return new Dictionary<CNTK.Variable, CNTK.Value>() { { variable, inputValue } };
        }


        /// <summary>
        /// ミニバッチ用に画像データとラベルデータを取得する
        /// </summary>
        /// <param name="trainingDataMap">学習用データリスト</param>
        /// <param name="batchSize">バッチサイズ</param>
        /// <param name="batchCount">何回目のバッチか？</param>
        /// <param name="width">画像の幅</param>
        /// <param name="height">画像の高さ</param>
        /// <param name="channel">画像のチャンネル数</param>
        /// <param name="numClasses">クラス数</param>
        /// <param name="device">デバイス（CPU or GPU）</param>
        /// <param name="imageBatch">画像データ</param>
        /// <param name="labelBatch">ラベルデータ</param>
        /// <returns></returns>
        public static bool GetImageAndLabelMinibatch(
            List<Tuple<string, int, float[]>> trainingDataMap, // 
            int batchSize,              // 
            int batchCount,             // 
                                        //int[] imageDims,            // 画像データサイズ　幅、高さ、チャンネル
            int width,
            int height,
            int channel,
            int numClasses,             // クラス数
                                        //DeviceDescriptor device,    // CPU or GPU
            out Value imageBatch,
            out Value labelBatch
            )
        {
            // バッチサイズ（データ個数の余りを考慮）
            int actualBatchSize = Math.Min(trainingDataMap.Count() - batchSize * batchCount, batchSize);
            // 全て学習し終わったら終了
            if (actualBatchSize <= 0)
            {
                imageBatch = null;
                labelBatch = null;
                return false;
            }

            // 最初のバッチのとき
            if (batchCount == 0)
            {
                // 学習用データをシャッフルする
                int n = trainingDataMap.Count;
                Random random = new Random(0);
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    var value = trainingDataMap[k];
                    trainingDataMap[k] = trainingDataMap[n];
                    trainingDataMap[n] = value;
                }
            }

            // ミニバッチごとのデータを取得
            int imageSize = width * height * channel;
            float[] batchImageBuf = new float[actualBatchSize * imageSize];
            float[] batchLabelBuf = new float[actualBatchSize * numClasses];
            for (int i = 0; i < actualBatchSize; i++)
            {
                int index = i + batchSize * batchCount;
                trainingDataMap[index].Item3.CopyTo(batchImageBuf, i * imageSize);
                for (int c = 0; c < numClasses; c++)
                {
                    if (c == trainingDataMap[index].Item2)
                    {
                        batchLabelBuf[i * numClasses + c] = 1;
                    }
                    else
                    {
                        batchLabelBuf[i * numClasses + c] = 0;
                    }
                }
            }

            // Value.CreateBatch<byte>がいけるかも？？
            imageBatch = Value.CreateBatch<float>(new int[] { width, height, channel }, batchImageBuf, Layers._device);
            labelBatch = Value.CreateBatch<float>(new int[] { numClasses }, batchLabelBuf, Layers._device);
            return true;
        }



    }
}
