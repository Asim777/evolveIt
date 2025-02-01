namespace Data.Neuron
{
    public abstract class SensorNeuron : Neuron, IInputNeuron
    {
        private SensorNeuron(string id, INeuronCategory category, float value) : base(id, category)
        {
            
        }
        // Obstacle Neurons
        public class ObstacleAhead : SensorNeuron
        {
            public ObstacleAhead() : base("Oah", SensorCategory.Obstacle, 0)
            {
                
            }
        }

        public class ObstacleAround : SensorNeuron
        {
            public ObstacleAround() : base("Oar", SensorCategory.Obstacle, 0)
            {

            }
        }

        public class ObstacleEncountered : SensorNeuron
        {
            public ObstacleEncountered() : base("Oe", SensorCategory.Obstacle, 0)
            {
                
            }
        }

        // Entity Neurons
        public class EntityAhead : SensorNeuron
        {
            public EntityAhead() : base("Eah", SensorCategory.Entity, 0)
            {
                
            }
        }

        public class EntityAround : SensorNeuron
        {
            public EntityAround() : base("Ear", SensorCategory.Entity, 0)
            {
                
            }
        }

        public class EntityEncountered : SensorNeuron
        {
            public EntityEncountered() : base("Ee", SensorCategory.Entity, 0)
            {
                
            }
        }
        
        // Food Neurons
        public class FoodAhead : SensorNeuron
        {
            public FoodAhead() : base("Fah", SensorCategory.Food, 0)
            {
                
            }
        }
        
        public class FoodAround : SensorNeuron
        {
            public FoodAround() : base("Far", SensorCategory.Food, 0)
            {
                
            }
        }
        
        public class FoodEncountered : SensorNeuron
        {
            public FoodEncountered() : base("Fe", SensorCategory.Food, 0)
            {
                
            }
        }

    }
}