namespace Data.Neuron
{
    public abstract class Neuron
    {
        public string Id { get; set; }
        public INeuronCategory Category { get; set;  }

        protected Neuron(string id, INeuronCategory category)
        {
            Id = id;
            Category = category;
        }
    }
    
    public interface IInputNeuron
    {
    }
    
    public interface IOutputNeuron
    {
    }
    
    // Extension method to get all neurons provided the ActivationGroup. We have 6 combinations of Neurons represented
    // by ActivationGroups each of which contains all Neurons from previous categories plus some new ones.
    public static class NeuronExtensions
    {
        public static Neuron[] GetNeurons(ActivationGroup activationGroup)
        {
            Neuron[] additionalNeurons = { };

            return activationGroup switch
            {
                ActivationGroup.AG13 =>
                new Neuron [] {
                    new SensorNeuron.ObstacleAhead(),
                    new SensorNeuron.ObstacleAround(),
                    new SensorNeuron.EntityAhead(),
                    new SensorNeuron.EntityAround(),
                    new SensorNeuron.FoodAhead(),
                    new SensorNeuron.FoodAround(),
                    new SensorNeuron.ObstacleEncountered(),
                    new SensorNeuron.EntityEncountered(),
                    new SensorNeuron.FoodEncountered(),
                    new SinkNeuron.Move(),
                    new SinkNeuron.Turn(),
                    new SinkNeuron.Eat(),
                    new SinkNeuron.Mate()
                },
                /*ActivationGroup.AG18 => GetNeurons(ActivationGroup.AG13).Concat(
                  new Neuron [] {
                            new ObstacleFront(),
                            new EntityFront(),
                            new FoodFront(),
                            new And(),
                            new Or()
                        ]
                },
                ActivationGroup.AG26 => GetNeurons(ActivationGroup.Ag18).Concat(
                new Neuron [] {
                            new ObstacleLeft(),
                            new EntityLeft(),
                            new FoodLeft(),
                            new ObstacleRight(),
                            new EntityRight(),
                            new FoodRight(),
                            new Less05(),
                            new More05()
                        
                },
                ActivationGroup.AG38 => GetNeurons(ActivationGroup.Ag26).Concat(
                new Neuron [] {
                            new ObstacleBehind(),
                            new EntityBehind(),
                            new FoodBehind(),
                            new GeneticSimilarityBehind(),
                            new GeneticSimilarityAhead
                            new GeneticSimilarityAround(),
                            new GeneticSimilarityFront(),
                            new GeneticSimilarityLeft(),
                            new GeneticSimilarityRight(),
                            new Less01(),
                            new More09(),
                            new Not()
                        ]
                },
                ActivationGroup.AG47 => GetNeurons(ActivationGroup.Ag38).Concat(
                new Neuron [] {
                            new EntityDensityAhead
                            new EntityDensityAround(),
                            new EntityDensityFront(),
                            new EntityDensityLeft(),
                            new EntityDensityRight(),
                            new EntityDensityBehind(),
                            new Xor(),
                            new Less025(),
                            new More075()
                        ]
                },
                ActivationGroup.AG55 => GetNeurons(ActivationGroup.A47).Concat(
                new Neuron [] {
                            new Hunger(),
                            new ReproductiveDrive(),
                            new Energy(),
                            new Health(),
                            new Age(),
                            new P(),
                            new Less075(),
                            new More025()
                        ]
                 },*/
                _ => throw new System.ArgumentException("Invalid Activation Group")
            };
        }
    }
}