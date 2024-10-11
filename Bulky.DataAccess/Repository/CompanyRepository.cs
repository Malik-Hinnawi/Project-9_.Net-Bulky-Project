using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class CompanyRepository : Repository<Company> , ICompanyRepository
{
    private ApplicationDbContext _db;

    public CompanyRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }
    
    public void Update(Company obj)
    {
        var objFromDb = _db.Companies.FirstOrDefault(u => u.Id == obj.Id);
        if (objFromDb != null)
        {
            objFromDb.Name = obj.Name;
            objFromDb.StreetAddress = obj.StreetAddress;
            objFromDb.City = obj.City;
            objFromDb.State = obj.State;
            objFromDb.PostalCode = obj.PostalCode;
            objFromDb.PhoneNumber = obj.PhoneNumber;
        }
    }
}