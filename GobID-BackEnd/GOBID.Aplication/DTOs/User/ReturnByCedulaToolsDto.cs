namespace GOBID.Application.DTOs.User;

public class ReturnByCedulaToolsViewModel
{
    public bool Valid { get; set; }
    public _Payload? Payload { get; set; }
}
public class _Payload
{
    public string? Names { get; set; }
    public string? FirstSurname { get; set; }
    public string? SecondSurname { get; set; }
    public char Gender { get; set; }
}