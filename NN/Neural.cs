using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;
using System.Collections;
using Accord;
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Kernels;
using Accord.Statistics.Links;
using Accord.Statistics.Models.Markov.Topology;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NeuralNetwork1
{
    /// <summary>
    /// Класс для хранения образа – входной массив сигналов на сенсорах, выходные сигналы сети, и прочее
    /// </summary>
    public class Sample
    {
        /// <summary>
        /// Входной вектор
        /// </summary>
        public double[] input = null;

        /// <summary>
        /// Выходной вектор, задаётся извне как результат распознавания
        /// </summary>
        public double[] output = null;

        /// <summary>
        /// Вектор ошибки, вычисляется по какой-нибудь хитрой формуле
        /// </summary>
        public double[] error = null;

        /// <summary>
        /// Действительный класс образа. Указывается учителем
        /// </summary>
        public DigitClass actualClass;

        /// <summary>
        /// Распознанный класс - определяется после обработки
        /// </summary>
        public DigitClass recognizedClass;

        /// <summary>
        /// Конструктор образа - на основе входных данных для сенсоров, при этом можно указать класс образа, или не указывать
        /// </summary>
        /// <param name="inputValues"></param>
        /// <param name="sampleClass"></param>
        public Sample(double[] inputValues, DigitClass sampleClass = DigitClass.Undef)
        {
            input = (double[]) inputValues.Clone();
            output = new double[10];
            if (sampleClass != DigitClass.Undef) output[(int)sampleClass] = 1;
            recognizedClass = DigitClass.Undef;
            actualClass = sampleClass;
        }

        /// <summary>
        /// Обработка реакции сети на данный образ на основе вектора выходов сети
        /// </summary>
        public void processOutput()
        {
            if (error == null)
                error = new double[output.Length];

            //  Нам так-то выход не нужен, нужна ошибка и определённый класс
            recognizedClass = 0;
            for (int i = 0; i < output.Length; ++i)
            {
                error[i] = ((i == (int) actualClass ? 1 : 0) - output[i]);
                if (output[i] > output[(int) recognizedClass]) recognizedClass = (DigitClass) i;
            }
        }

        /// <summary>
        /// Добавляет к аргументу ошибку, соответствующую данному образу (не квадратичную!!!)
        /// </summary>
        /// <param name="errorVector"></param>
        /// <returns></returns>
        public void updateErrorVector(double[] errorVector)
        {
            for (int i = 0; i < errorVector.Length; ++i)
                errorVector[i] += error[i];
        }

        /// <summary>
        /// Представление в виде строки
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string result = "Sample decoding : " + actualClass.ToString() + "(" + ((int)actualClass).ToString() + "); " + Environment.NewLine + "Input : ";
            for (int i = 0; i < input.Length; ++i) result += input[i].ToString() + "; ";
            result += Environment.NewLine + "Output : ";
            if (output == null) result += "null;";
            else
                for (int i = 0; i < output.Length; ++i) result += output[i].ToString() + "; ";
            result += Environment.NewLine + "Error : ";

            if (error == null) result += "null;";
            else
                for (int i = 0; i < error.Length; ++i) result += error[i].ToString() + "; ";
            result += Environment.NewLine + "Recognized : " + recognizedClass.ToString() + "(" + ((int)recognizedClass).ToString() + "); " + Environment.NewLine;


            return result;
        }
        
        /// <summary>
        /// Правильно ли распознан образ
        /// </summary>
        /// <returns></returns>
        public bool Correct() { return actualClass == recognizedClass; }
    }
    
    /// <summary>
    /// Выборка образов. Могут быть как классифицированные (обучающая, тестовая выборки), так и не классифицированные (обработка)
    /// </summary>
    public class SamplesSet : IEnumerable
    {
        /// <summary>
        /// Накопленные обучающие образы
        /// </summary>
        public List<Sample> samples = new List<Sample>();

        public SamplesSet(string pathToDataset, int size = 2500)
        {
            using (var reader = new StreamReader(pathToDataset))
            {
                int i = 0;
                while (!reader.EndOfStream)
                {
                    var tmp = reader.ReadLine();
                    var line = tmp.Split(',');
                    string[] data = line[1].Split(' ');
                    int label = Convert.ToInt32(line[0]);
                    double[] img = new double[data.Length];
                    img = Array.ConvertAll(data, double.Parse);
                    Sample sample = new Sample(img, (DigitClass) label);
                    this.samples.Add(sample);
                    if (i > size)
                        break;
                    i += 1;
                    if (i > 10000)
                        break;
                    Console.WriteLine(i.ToString());
                }
            }
        }

        /// <summary>
        /// Добавление образа к коллекции
        /// </summary>
        /// <param name="image"></param>
        public void AddSample(Sample image)
        {
            samples.Add(image);
        }
        public int Count { get { return samples.Count; } }

        public IEnumerator GetEnumerator()
        {
            return samples.GetEnumerator();
        }

        /// <summary>
        /// Реализация доступа по индексу
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Sample this[int i]
        {
            get { return samples[i]; }
            set { samples[i] = value; }
        }

        public double ErrorsCount()
        {
            double correct = 0;
            double wrong = 0;
            foreach (var sample in samples)
                if (sample.Correct()) ++correct; else ++wrong;
            return correct / (correct + wrong);
        }
        // Тут бы ещё сохранение в файл и чтение сделать, вообще классно было бы
    }

    public class Cache
    {
        public double[, ] layerIn;
        public double[, ] layerWeights;
        public double[] layerBiases;

        public Cache(double[, ] layerIn, double[, ] layerWeights, double[] layerBiases)
        {
            this.layerIn = layerIn;
            this.layerWeights = layerWeights;
            this.layerBiases = layerBiases;
        }
    }

    public abstract class Layer
    {
        public abstract double[, ] Forward(double[, ] input);
        public abstract double[, ] Backward(double[, ] dout);
        public abstract void update(double[,] dw, double db);

        public abstract void ToFile(string prefix);
        public void Serialize(object t, string path)
        {
            using(Stream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                bformatter.Serialize(stream, t);
            }
        }
        
        public object Deserialize(string path) 
        {
            using(Stream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                return bformatter.Deserialize(stream);
            }
        }

    }
    public class AffineLayer : Layer
    {
        private double learningRate = 1e-6;
        private double[, ] weights;
        private double[] biases;
        private Cache cache;

        public AffineLayer(int inputDim, int outputDim)
        {
            weights = new double[inputDim, outputDim];
            biases = new double[outputDim];
            cache = null;
            NormalDistribution gaussianNormalDistribution = NormalDistribution.Standard;
            for (int i = 0; i < inputDim; i++)
            {
                for (int j = 0; j < outputDim; j++)
                {
                    weights[i, j] = gaussianNormalDistribution.Generate();
                    biases[j] = 0.0;
                }
            }
        }

        public AffineLayer(string prefix)
        {
            this.weights = (double[,]) base.Deserialize(prefix + "affine.dat");
            this.biases = (double[]) base.Deserialize(prefix + "bias.dat");
            this.cache = null;
        }

        public override double[, ] Forward(double[, ] input)
        {
            this.cache = new Cache(input, weights, biases);
            double[,] output = input.Dot(weights);
            output = output.Add(biases.Transpose());
            return output;
        }

        public override void update(double[,] dw, double db)
        {
            double[,] dwMult = dw.Multiply(learningRate);
            this.weights = this.weights.Subtract(dwMult);
            this.biases = this.biases.Subtract(db*learningRate);
        }

        /// <summary>
        /// Returns dx partial derivative
        /// </summary>
        /// <param name="dout"> upstream derivative</param>
        /// <returns></returns>
        public override double[, ] Backward(double[, ] dout)
        {
            double[,] wt = this.cache.layerWeights.Transpose();
            double[, ] dx = dout.Dot(wt);
            double[, ] xt = this.cache.layerIn.Transpose();
            double[, ] dw = xt.Dot(dout);
            double db = dout.Sum();
            update(dw, db);
            this.cache = null;
            return dx;
        }

        public override void ToFile(string prefix)
        {
            base.Serialize(this.weights, prefix + "affine.dat");
            base.Serialize(this.biases, prefix+"bias.dat");
        }
    }

    public class ReLu: Layer
    {
        private double[, ] cache = null;
        
        public ReLu() {}

        public override double[, ] Forward(double[, ] input)
        {
            double[, ] output = new double[input.GetLength(0), input.GetLength(1)];
            for (int i = 0; i < input.GetLength(0); i++)
            {
                for (int j = 0; j < input.GetLength(1); j++)
                    output[i, j] = System.Math.Max(input[i, j], 0.0);
            }

            this.cache = input;
            return output;
        }

        public override void update(double[,] dw, double db)
        {
            throw new NotImplementedException();
        }

        public override void ToFile(string prefix)
        {
            throw new NotImplementedException();
        }

        public override double[,] Backward(double[, ] dout)
        {
            double[, ] dx = new double[dout.GetLength(0), dout.GetLength(1)];
            for (int i = 0; i < dout.GetLength(0); i++)
                for (int j = 0; j < dout.GetLength(1); j++)
                    if (this.cache[i, j].CompareTo(0.0) >= 0)
                        dx[i, j] = dout[i, j]; 
                return dx;
        }
    }
    
    public class NeuralNetwork : BaseNetwork
    {
        private List<Layer> layers;
        private double[] output;
            public NeuralNetwork(int[] structure)
            {
                layers = new List<Layer>();
                for (int i = 1; i < structure.Length - 1; i++)
                {
                    layers.Add(new AffineLayer(structure[i-1],structure[i]));
                    layers.Add(new ReLu());
                }
                layers.Add(new AffineLayer(structure[structure.Length - 2], 
                    structure[structure.Length - 1]));
            }

            public NeuralNetwork(string path)
            {
                layers = new List<Layer>();
                string[] config = File.ReadAllLines(path + "config.dat");
                for(int i = 0; i < config.Length - 1; i++)
                {
                    layers.Add(new AffineLayer(config[i]));
                    layers.Add(new ReLu());
                }

                layers.Add(new AffineLayer(config[config.Length - 1]));

            }

            public override void Serialize(string path)
            {
                List<string> config = new List<string>();
                for(int i = 0; i < layers.Count; i++)
                {
                    if (layers[i] is AffineLayer)
                    {
                        layers[i].ToFile(path + i.ToString());
                        config.Add(path + i.ToString());
                    }
                }

                System.IO.File.WriteAllLines(path + "config.dat", config);
            }
            
            public override void ReInit(int[] structure, double initialLearningRate = 0.25)
            {
                layers = new List<Layer>();
                for (int i = 1; i < structure.Length - 1; i++)
                {
                    layers.Add(new AffineLayer(structure[i-1],structure[i]));
                    layers.Add(new ReLu());
                }
                layers.Add(new AffineLayer(structure[structure.Length - 2], 
                    structure[structure.Length - 1]));
            }

            private Tuple<double, double[, ]> SoftmaxLoss(double[, ] samples, int label)
            {
                double[] samplesMax = new double[1];
                samplesMax = samples.Max(1, samplesMax);
                
                double[,] shiftedLogits = new double[1, samples.GetLength(1)];
                for (int i = 0; i < samples.GetLength(1); i++)
                    shiftedLogits[0, i] = samples[0, i] - samplesMax[0];
                
                double zLog = Math.Log(shiftedLogits.Exp().Sum());
                double[,] logProbs = shiftedLogits.Subtract(zLog);
                
                double loss = -logProbs[0, label];
                
                double[, ] dx = logProbs.Exp();
                dx[0, label] -= 1;
                return new Tuple<double, double[, ]>(loss, dx);
            }

            private double[, ] ForwardPass(Sample sample)
            {
                double[,] input = new double[1, sample.input.Length];
                
                for (int j = 0; j < sample.input.Length; j++)
                    input[0, j] = sample.input[j];

                double[,] scores = input;
                for (int i = 0; i < layers.Count; i++)
                    scores = layers[i].Forward(scores);
                
                sample.output = scores.GetRow(0);
                
                sample.processOutput();
                this.output = scores.GetRow(0);
                // double outputMax = scores.Max();
                // double outputMin = scores.Min();
                // scores = scores.Subtract(scores.Min());
                // scores = scores.Divide(outputMax - outputMin); // :TODO Need to check if output in [0; 1]
                return scores;
            }
            private double BackwardPass(Sample sample, double[, ] scores)
            {
                int label = (int) sample.actualClass;
                Tuple<double, double[, ]> softmaxLoss = SoftmaxLoss(scores, label);
                if (softmaxLoss.Item1.CompareTo(0.0) != 0)
                {
                    double[,] dx = softmaxLoss.Item2;
                    for (int i = layers.Count - 1; i >= 0; i--)
                        dx = layers[i].Backward(dx);
                }
                return softmaxLoss.Item1;
            }
            public override int Train(Sample sample, bool parallel = true)
            {
                int iterationsCnt = 0;
                double currentLoss = Double.MaxValue;
                // while (iterationsCnt < 100)
                // {
                    double[,] scores = ForwardPass(sample);
                    if (currentLoss < 0.8 && sample.Correct())
                        return iterationsCnt;

                    currentLoss = BackwardPass(sample, scores);
                    Console.WriteLine(currentLoss.ToString());
                    iterationsCnt += 1;
                // }
                return iterationsCnt;
            }

            public override double TrainOnDataSet(SamplesSet samplesSet, int epochsCount, double acceptableErorr,
                bool parallel = true)
            {
                double recognized = 0.0;
                while (epochsCount > 0)
                {
                    Console.WriteLine("now on " + epochsCount.ToString() + " epoch");
                    recognized = 0;
                    for (int i = 0; i < samplesSet.samples.Count; ++i)
                        if (Train(samplesSet.samples.ElementAt(i)) == 0)
                            recognized += 1;
                    recognized /= samplesSet.samples.Count;
                    if (recognized > acceptableErorr) return recognized;
                    epochsCount--;
                }
                return recognized;
            }

            public override DigitClass Predict(Sample sample)
            {
                ForwardPass(sample);
                return sample.recognizedClass;
            }

            public override double TestOnDataSet(SamplesSet testSet)
            {
                double recognized = 0;
                for (int i = 0; i < testSet.Count; ++i)
                {
                    Sample s = testSet.samples.ElementAt(i);
                    Predict(s);
                    if (s.Correct()) recognized += 1;
                }
                return recognized / testSet.Count;
            }

            public override double[] getOutput()
            {
                return this.output;
            }
        }

    }
