namespace GOBID.Application.DTOs.DashBoard;

public record SuccessfulLoginsByMonthDto
{
    public int MonthNumber { get; set; }       
    public required string MonthName { get; set; }      
    public int SuccessfulLogins { get; set; }  
}
