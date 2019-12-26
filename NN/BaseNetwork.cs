using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetwork1
{
    /// <summary>
    /// Базовый класс для реализации как самодельного персептрона, так и обёртки для ActivationNetwork из Accord.Net
    /// </summary>
    ///
    public enum DigitClass : int { Undef = -1, Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9};
    abstract public class BaseNetwork
    {
        //  Делегат для информирования о процессе обучения (периодически извещает форму о том, сколько процентов работы сделано)
        //  public FormUpdater updateDelegate = null;

        public abstract void ReInit(int[] structure, double initialLearningRate = 0.25);

        public abstract int Train(Sample sample, bool parallel = true);

        public abstract double TrainOnDataSet(SamplesSet samplesSet, int epochs_count, double acceptable_erorr, bool parallel = true);

        public abstract DigitClass Predict(Sample sample);

        public abstract double TestOnDataSet(SamplesSet testSet);

        public abstract double[] getOutput();

        public abstract void Serialize(string path);
    }
}
