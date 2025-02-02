using Data.Neuron;

namespace Data.Gene
{
    /// <summary>
    /// NeuronConnection is a relationship between two Neurons - Input and Output.
    /// Input Neuron is the Neuron that provides some information. It can be either Sensor or Input Inner Neuron
    /// Output Neuron is the Neuron that receives some information. It can be either Sink or Output Inner Neuron
    /// </summary>
    public record NeuronConnection(
        IInputNeuron Input,
        IOutputNeuron Output
    )
    {
        public IInputNeuron Input { get; set; } = Input;
        public IOutputNeuron Output { get; set; } = Output;
    }
}