using System.Collections.Generic;

namespace Physics
{
    public class CollisionMatrix
    {
        private readonly Dictionary<int, int> _data = new Dictionary<int, int>();
        private readonly Dictionary<string, int> _layers = new Dictionary<string, int>();

        public CollisionMatrix(IList<object> table)
        {
            IList<object> names = (IList<object>) table[0];
            List<int> layers = new List<int>();

            if (names.Count > 0)
            {
                layers.Add(0);
                int layer = 1;
                for (int i = 1; i < names.Count; i++)
                {
                    _layers.Add(names[i].ToString(), layer);

                    layers.Add(layer);
                    layer *= 2;
                }
            }

            for (int i = 1; i < layers.Count; i++)
            {
                int result = 0;
                IList<object> layerData = (IList<object>) table[layers.Count - i];
                for (int j = 1; j < layerData.Count; j++)
                {
                    if ((bool) layerData[j])
                    {
                        result |= layers[j];
                    }
                }

                int k = i + 1;
                for (int j = layers.Count - i - 1; j > 1; j--, k++)
                {
                    if ((bool) ((IList<object>) table[j])[i])
                    {
                        result |= layers[k];
                    }
                }

                _data.Add(layers[i], result);
            }
        }

        public int GetLayer(string id)
        {
            return _layers[id];
        }

        public bool Check(int layer1, int layer2)
        {
            return (_data[layer1] & layer2) == layer2 || (_data[layer2] & layer1) == layer1;
        }
    }
}