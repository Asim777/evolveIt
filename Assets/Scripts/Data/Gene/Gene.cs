using System.Collections.Generic;
using System.Linq;

namespace Data.Gene
{
    public class Gene
    {
        public List<Neuron.Neuron> Sensors { get; set; }
        public Neuron.Neuron Inner { get; set; }
        public Neuron.Neuron Sink { get; set; }
        public string Name { get; set; }

        public Gene(List<Neuron.Neuron> sensors, Neuron.Neuron inner, Neuron.Neuron sink)
        {
            Sensors = sensors;
            Inner = inner;
            Sink = sink;
            Name = string.Join(",", sensors.Select(s => s.Id)) + "_" + (Inner?.Id ?? "") + "_" + Sink.Id;
        }

        public bool ContainsNeuron(string neuronName)
        {
            return Sensors.Any(s => s.Id == neuronName) || Inner?.Id == neuronName || Sink.Id == neuronName;
        }

        public Neuron.Neuron GetNeuron(string neuronName)
        {
            var neuron = Sensors.FirstOrDefault(s => s.Id == neuronName);
            if (neuron == null && Inner?.Id == neuronName)
            {
                neuron = Inner;
            }
            else if (neuron == null && Sink.Id == neuronName)
            {
                neuron = Sink;
            }
            return neuron;
        }
    }
}