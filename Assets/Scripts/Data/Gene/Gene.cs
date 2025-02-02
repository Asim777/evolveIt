using System.Collections.Generic;
using System.Linq;

namespace Data.Gene
{
    public class Gene
    {
        private List<Neuron.Neuron> Sensors { get; }
        private Neuron.Neuron Inner { get; }
        private Neuron.Neuron Sink { get; }
        public string Name { get; }

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
        
        public List<Neuron.Neuron> GetAllNeurons()
        {
            var neurons = Sensors.Concat(new List<Neuron.Neuron> { Inner, Sink })
                .Where(n => n != null)
                .ToList();
            return neurons;
        }
    }
}