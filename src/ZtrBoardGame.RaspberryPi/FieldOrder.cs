namespace ZtrBoardGame.RaspberryPi;

public record FieldOrder(IReadOnlyCollection<int> order)
{
    public IReadOnlyCollection<int> Order => order;
}
