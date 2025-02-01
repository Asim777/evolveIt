namespace Data.Neuron
{
    public abstract class SinkNeuron : Neuron, IOutputNeuron
    {
        private SinkNeuron(string id, INeuronCategory category) : base(id, category)
        {
        }

        public class Move : SinkNeuron
        {
            public Move() : base("M", SinkCategory.Movement)
            {
            }
        }

        public class Turn : SinkNeuron
        {
            public Turn() : base("T", SinkCategory.Turn)
            {
            }
        }

        public class Eat : SinkNeuron
        {
            public Eat() : base("E", SinkCategory.Eat)
            {
            }
        }

        public class Mate : SinkNeuron
        {
            public Mate() : base("M", SinkCategory.Mate)
            {
            }
        }
    }
}