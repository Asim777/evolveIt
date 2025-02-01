namespace Data.Neuron
{
    public abstract class InnerNeuron : Neuron, IInputNeuron, IOutputNeuron
    {
        private InnerNeuron(string id, INeuronCategory category) : base(id, category)
        {
            
        }
    }
}
