using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.optim;

namespace LaserTagBox.Model.Mind;

public class QTrainer
{
    public QTrainer(Module<Tensor, Tensor> model, double lr, double gamma)
    {
        _lr = lr;
        _gamma = gamma;
        _model = model;
        _optimizer = Adam(model.parameters(), lr: lr);
        _criterion = MSELoss();
    }

    public void TrainStep(int[] state, int[] action, int reward, int[] nextState, bool done)
    {
        var stateTensor = tensor(state, dtype: float32);
        var nextStateTensor = tensor(nextState, dtype: float32);
        var actionTensor = tensor(action, dtype: int64);
        var rewardTensor = tensor(reward, dtype: float32);

        if (stateTensor.shape.Length == 1)
        {
            stateTensor = unsqueeze(stateTensor, 0);
            nextStateTensor = unsqueeze(nextStateTensor, 0);
            actionTensor = unsqueeze(actionTensor, 0);
            rewardTensor = unsqueeze(rewardTensor, 0);
        }

        var pred = _model.forward(stateTensor);
        var target = pred.clone();

        var qNew = rewardTensor[0];
        if (done == false)
        {
            qNew = rewardTensor[0].add(_gamma * max(_model.forward(nextStateTensor[0])));
        }

        target[0][argmax(actionTensor[0]).item<long>()] = qNew;

        _optimizer.zero_grad();
        var loss = _criterion.forward(target, pred);
        loss.backward();
        _optimizer.step();
    }

    private double _lr;
    private readonly Module<Tensor, Tensor> _model;
    private readonly Adam _optimizer;
    private readonly MSELoss _criterion;
    private readonly double _gamma;
}