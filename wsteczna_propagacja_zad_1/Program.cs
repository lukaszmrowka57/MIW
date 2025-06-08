using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace XORApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new XORForm());
        }
    }

    public class XORForm : Form
    {
        private TextBox txtResults;
        private NeuralNetwork network;

        public XORForm()
        {
            this.Text = "XOR - Sieć Neuronowa";
            this.Width = 900;
            this.Height = 700;

            Label lbl = new Label()
            {
                Text = "Trening sieci neuronowej XOR",
                AutoSize = true,
                Font = new Font("Segoe UI", 18),
                Location = new Point(20, 20)
            };
            this.Controls.Add(lbl);

            Button trainButton = new Button()
            {
                Text = "Trenuj",
                Font = new Font("Segoe UI", 16),
                Size = new Size(240, 70),
                Location = new Point(20, 80)
            };
            trainButton.Click += TrainButton_Click;
            this.Controls.Add(trainButton);

            txtResults = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 14),
                Size = new Size(820, 460),
                Location = new Point(20, 170),
                ReadOnly = true
            };
            this.Controls.Add(txtResults);
        }

        private void TrainButton_Click(object sender, EventArgs e)
        {
            txtResults.Text = "Rozpoczynanie treningu...\r\n";

            List<double[]> trainingData = new List<double[]>
            {
                new double[] {0, 0, 0},
                new double[] {1, 0, 1},
                new double[] {1, 1, 0}
            };

            network = new NeuralNetwork(new int[] { 2, 2, 1 }, beta: 1.0, learningRate: 0.3);
            network.Train(trainingData, 50000);

            txtResults.AppendText("Trening zakończony.\r\n\r\nWyniki:\r\n");

            using (StreamWriter sw = new StreamWriter("wyniki_XOR.txt"))
            {
                foreach (var sample in trainingData)
                {
                    double[] input = new double[] { sample[0], sample[1] };
                    double[] output = network.FeedForward(input);
                    double error = Math.Abs(sample[2] - output[0]);

                    string line = $"Wejście: {sample[0]} {sample[1]} → Wyjście: {output[0]:0.000} (oczekiwane: {sample[2]}) Błąd: {error:0.000}\r\n";
                    txtResults.AppendText(line);
                    sw.Write(line);
                }

                sw.WriteLine("\nTrening zakończony.");
            }

            MessageBox.Show("Zapisano wyniki do pliku: wyniki_XOR.txt", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class Neuron
    {
        public double[] Weights;
        public double Bias;
        public double Output;

        public Neuron(int inputCount, Random random)
        {
            Weights = new double[inputCount];
            for (int i = 0; i < inputCount; i++)
                Weights[i] = random.NextDouble() * 10 - 5;
            Bias = random.NextDouble() * 10 - 5;
        }
    }

    public class Layer
    {
        public List<Neuron> Neurons;

        public Layer(int neuronCount, int inputCount, Random random)
        {
            Neurons = new List<Neuron>();
            for (int i = 0; i < neuronCount; i++)
                Neurons.Add(new Neuron(inputCount, random));
        }
    }

    public class NeuralNetwork
    {
        public List<Layer> Layers;
        private double Beta;
        private double LearningRate;
        private Random random = new Random();

        public NeuralNetwork(int[] layerSizes, double beta, double learningRate)
        {
            Beta = beta;
            LearningRate = learningRate;
            Layers = new List<Layer>();

            for (int i = 0; i < layerSizes.Length; i++)
            {
                int inputs = (i == 0) ? layerSizes[i] : layerSizes[i - 1];
                Layers.Add(new Layer(layerSizes[i], inputs, random));
            }
        }

        private double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-Beta * x));
        }

        private double SigmoidDerivative(double y)
        {
            return Beta * y * (1 - y);
        }

        public double[] FeedForward(double[] inputs)
        {
            double[] outputs = inputs;
            foreach (var layer in Layers)
            {
                double[] newOutputs = new double[layer.Neurons.Count];
                for (int i = 0; i < layer.Neurons.Count; i++)
                {
                    var neuron = layer.Neurons[i];
                    double sum = neuron.Bias;
                    for (int j = 0; j < neuron.Weights.Length; j++)
                        sum += neuron.Weights[j] * outputs[j];
                    neuron.Output = Sigmoid(sum);
                    newOutputs[i] = neuron.Output;
                }
                outputs = newOutputs;
            }
            return outputs;
        }

        public void Train(List<double[]> data, int epochs)
        {
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                foreach (var sample in data)
                    BackPropagate(new double[] { sample[0], sample[1] }, new double[] { sample[2] });
            }
        }

        private void BackPropagate(double[] inputs, double[] targets)
        {
            FeedForward(inputs);

            double[][] deltas = new double[Layers.Count][];
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                deltas[i] = new double[Layers[i].Neurons.Count];

                for (int j = 0; j < Layers[i].Neurons.Count; j++)
                {
                    var neuron = Layers[i].Neurons[j];
                    if (i == Layers.Count - 1)
                    {
                        deltas[i][j] = (targets[j] - neuron.Output) * SigmoidDerivative(neuron.Output);
                    }
                    else
                    {
                        double sum = 0;
                        for (int k = 0; k < Layers[i + 1].Neurons.Count; k++)
                            sum += Layers[i + 1].Neurons[k].Weights[j] * deltas[i + 1][k];
                        deltas[i][j] = sum * SigmoidDerivative(neuron.Output);
                    }
                }
            }

            for (int i = 0; i < Layers.Count; i++)
            {
                double[] layerInputs = i == 0 ? inputs : Layers[i - 1].Neurons.Select(n => n.Output).ToArray();

                for (int j = 0; j < Layers[i].Neurons.Count; j++)
                {
                    var neuron = Layers[i].Neurons[j];
                    for (int k = 0; k < neuron.Weights.Length; k++)
                        neuron.Weights[k] += LearningRate * deltas[i][j] * layerInputs[k];
                    neuron.Bias += LearningRate * deltas[i][j];
                }
            }
        }
    }
}
