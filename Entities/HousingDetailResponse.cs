namespace SimpleWebAppReact.Entities;

public class HousingDetailResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Acronym { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Image { get; set; }
    public string? BuildingType { get; set; }
    public string? Description { get; set; }
    public HousingContact? Contact { get; set; }
    public List<decimal> Pricing { get; set; } = new();
    public List<Room> RoomOptions { get; set; } = new();
    public List<Room> AvailableListings { get; set; } = new();
    public List<string> FloorPlans { get; set; } = new();
    public List<string> Photos { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public int RoomCount { get; set; }
    public decimal? AveragePrice { get; set; }
}

public class HousingContact
{
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}
