using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.nn.functional;

namespace LaserTagBox.Model.Mind;

public class LinearQNet : Module<Tensor, Tensor>
{
    public LinearQNet(int inputSize, int hiddenSize, int outputSize) : base("LinearQNet")
    {
        _linear1 = Linear(inputSize, hiddenSize);
        _linear2 = Linear(hiddenSize, outputSize);

        RegisterComponents();
    }

    public override Tensor forward(Tensor x)
    {
        x = relu(_linear1.forward(x));
        x = _linear2.forward(x);
        return x;
    }

    private readonly Linear _linear1;
    private readonly Linear _linear2;
}