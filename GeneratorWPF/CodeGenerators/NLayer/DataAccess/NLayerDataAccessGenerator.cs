using GeneratorWPF.Extensions;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace GeneratorWPF.CodeGenerators.NLayer.DataAccess;

public class NLayerDataAccessGenerator
{
    private readonly EntityRepository _entityRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly RelationRepository _relationRepository;
    private readonly AppSetting _appSetting;
    public NLayerDataAccessGenerator(AppSetting appSetting)
    {
        _appSetting = appSetting;
        _entityRepository = new();
        _fieldRepository = new();
        _relationRepository = new();
    }

    public string CreateProject(string path, string solutionName)
    {
        try
        {
            string projectPath = Path.Combine(path, "DataAccess");
            string csprojPath = Path.Combine(projectPath, "DataAccess.csproj");

            if (Directory.Exists(projectPath) && File.Exists(csprojPath))
                return "INFO: DataAccess layer project already exists.";

            RunCommand(path, "dotnet", "new classlib -n DataAccess");
            RunCommand(path, "dotnet", $"sln {solutionName}.sln add DataAccess/DataAccess.csproj");
            RunCommand(projectPath, "dotnet", $"dotnet add reference ../Model/Model.csproj");

            RemoveFile(projectPath, "Class1.cs");
            return "OK: DataAccess Project Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the DataAccess project. \n\t Details:{ex.Message}");
        }
    }

    #region Package Methods
    public string AddPackage(string path, string packageName)
    {
        try
        {
            string projectPath = Path.Combine(path, "DataAccess");
            string csprojPath = Path.Combine(projectPath, "DataAccess.csproj");

            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"DataAccess.csproj not found for adding package({packageName}).");

            var doc = XDocument.Load(csprojPath);

            var packageAlreadyAdded = doc.Descendants("PackageReference").Any(p => p.Attribute("Include")?.Value == packageName);

            if (packageAlreadyAdded)
                return $"INFO: Package {packageName} already exists in DataAccess project.";

            RunCommand(projectPath, "dotnet", $"add package {packageName}");

            return $"OK: Package {packageName} added to DataAccess project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding pacgace to DataAccess project. \n\t Details:{ex.Message}");
        }
    }
    public string Restore(string path)
    {
        try
        {
            string projectPath = Path.Combine(path, "DataAccess");

            RunCommand(projectPath, "dotnet", "restore");
            return "OK: Restored DataAccess project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while restoring DataAccess project. \n Details:{ex.Message}");
        }
    }
    #endregion

    #region Static Files
    public string GenerateRepositoryBase(string solutionPath)
    {
        var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository); 

        string contextRule = "DbContext";
        if (_appSetting.IsThereIdentiy)
        {
            contextRule = $"IdentityDbContext<{identityTypeConfigs.IdentityUserType}, {identityTypeConfigs.IdentityRoleType}, {identityTypeConfigs.IdentityKeyType}>";
        }

        string code_IRepository = @"using AutoMapper;
using Core.Model;
using Core.Utils.Datatable;
using Core.Utils.DynamicQuery;
using Core.Utils.Pagination;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace DataAccess.Repository;

public interface IRepository<TEntity> where TEntity : IEntity
{
    #region Add
    TEntity Add(TEntity entity);
    List<TEntity> Add(IEnumerable<TEntity> entities);

    TEntity AddAndSave(TEntity entity);
    List<TEntity> AddAndSave(IEnumerable<TEntity> entities);
    #endregion

    #region Update
    TEntity Update(TEntity entity);
    List<TEntity> Update(IEnumerable<TEntity> entities);

    TEntity UpdateAndSave(TEntity entity);
    List<TEntity> UpdateAndSave(IEnumerable<TEntity> entities);
    #endregion

    #region Delete
    void Delete(TEntity entity);
    void Delete(IEnumerable<TEntity> entities);
    void Delete(Expression<Func<TEntity, bool>> where);

    void DeleteAndSave(TEntity entity);
    void DeleteAndSave(IEnumerable<TEntity> entities);
    void DeleteAndSave(Expression<Func<TEntity, bool>> where);
    #endregion

    #region IsExist & Count
    bool IsExist(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false);
    int Count(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false);
    #endregion

    #region Get
    TEntity? Get(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true
    );

    TResult? Get<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    );

    TResult? Get<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    );
    #endregion

    #region GetAll
    ICollection<TEntity>? GetAll(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true
    );

    ICollection<TResult>? GetAll<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    );

    ICollection<TResult>? GetAll<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    );
    #endregion

    #region Datatable Server-Side
    DatatableResponseServerSide<TEntity> DatatableServerSide(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );

    DatatableResponseServerSide<TResult> DatatableServerSide<TResult>(
        DatatableRequest datatableRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );

    DatatableResponseServerSide<TResult> DatatableServerSide<TResult>(
        DatatableRequest datatableRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );
    #endregion

    #region Datatable Client-Side
    DatatableResponseClientSide<TEntity> DatatableClientSide(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );

    DatatableResponseClientSide<TResult> DatatableClientSide<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );

    DatatableResponseClientSide<TResult> DatatableClientSide<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );
    #endregion

    #region Pagination
    PaginationResponse<TEntity> Pagination(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );

    PaginationResponse<TResult> Pagination<TResult>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );

    PaginationResponse<TResult> Pagination<TResult>(
        PaginationRequest paginationRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false
    );
    #endregion
}";

        string code_IRepositoryAsync = @"using AutoMapper;
using Core.Model;
using Core.Utils.Datatable;
using Core.Utils.DynamicQuery;
using Core.Utils.Pagination;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace DataAccess.Repository;

public interface IRepositoryAsync<TEntity> where TEntity : IEntity
{
    #region Add
    Task<TEntity> AddAndSaveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<List<TEntity>> AddAndSaveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    #endregion

    #region Update
    Task<TEntity> UpdateAndSaveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<List<TEntity>> UpdateAndSaveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    #endregion

    #region Delete
    Task DeleteAndSaveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAndSaveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task DeleteAndSaveAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default);
    #endregion

    #region IsExist & Count
    Task<bool> IsExistAsync(
        Filter? filter = null,
        Expression<Func<TEntity, bool>>? where = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );
    Task<int> CountAsync(
        Filter? filter = null,
        Expression<Func<TEntity, bool>>? where = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );
    #endregion

    #region Get
    Task<TEntity?> GetAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default
    );

    Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default
    );

    Task<TResult?> GetAsync<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default);
    #endregion

    #region GetAll
    Task<ICollection<TEntity>?> GetAllAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default
    );

    Task<ICollection<TResult>?> GetAllAsync<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default
    );

    Task<ICollection<TResult>?> GetAllAsync<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default
    );
    #endregion

    #region Datatable Server-Side
    Task<DatatableResponseServerSide<TEntity>> DatatableServerSideAsync(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );

    Task<DatatableResponseServerSide<TResult>> DatatableServerSideAsync<TResult>(
        DatatableRequest datatableRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );

    Task<DatatableResponseServerSide<TResult>> DatatableServerSideAsync<TResult>(
        DatatableRequest datatableRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );
    #endregion

    #region Datatable Client-Side
    Task<DatatableResponseClientSide<TEntity>> DatatableClientSideAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );

    Task<DatatableResponseClientSide<TResult>> DatatableClientSideAsync<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );

    Task<DatatableResponseClientSide<TResult>> DatatableClientSideAsync<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );
    #endregion

    #region Pagination
    Task<PaginationResponse<TEntity>> PaginationAsync(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );

    Task<PaginationResponse<TResult>> PaginationAsync<TResult>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );

    Task<PaginationResponse<TResult>> PaginationAsync<TResult>(
        PaginationRequest paginationRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default
    );
    #endregion
}";
        
        string code_RepositoryBase_1 = @"using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Model;
using Core.Utils.Datatable;
using Core.Utils.DynamicQuery;
using Core.Utils.Pagination;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Model.Entities;
using System.Linq.Expressions;

namespace DataAccess.Repository;

public class RepositoryBase<TEntity, TContext> : IRepository<TEntity>, IRepositoryAsync<TEntity>
    where TEntity : class, IEntity
    where TContext : "; 
    
        var code_RepositoryBase_2 = @"
    // DbContext 
    // IdentityDbContext<User, IdentityRole<Guid>, Guid> && IdentityDbContext<User, Role<Guid>, Guid>
{
    protected TContext _context { get; set; }
    public RepositoryBase(TContext context) => _context = context;


    // ############################# Sync Methods #############################
    #region Add
    public TEntity Add(TEntity entity)
    {
        _context.Set<TEntity>().Add(entity);
        return entity;
    }

    public List<TEntity> Add(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().AddRange(entities);
        return entities.ToList();
    }

    public TEntity AddAndSave(TEntity entity)
    {
        _context.Set<TEntity>().Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public List<TEntity> AddAndSave(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().AddRange(entities);
        _context.SaveChanges();
        return entities.ToList();
    }
    #endregion

    #region Update
    public TEntity Update(TEntity entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return entity;
    }

    public List<TEntity> Update(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().UpdateRange(entities);
        return entities.ToList();
    }

    public TEntity UpdateAndSave(TEntity entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        _context.SaveChanges();
        return entity;
    }

    public List<TEntity> UpdateAndSave(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().UpdateRange(entities);
        _context.SaveChanges();
        return entities.ToList();
    }
    #endregion

    #region Delete
    public void Delete(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
    }

    public void Delete(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().RemoveRange(entities);
    }

    public void Delete(Expression<Func<TEntity, bool>> where)
    {
        var entitiesToDelete = _context.Set<TEntity>().Where(where);
        _context.Set<TEntity>().RemoveRange(entitiesToDelete);
    }

    public void DeleteAndSave(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
        _context.SaveChanges();
    }

    public void DeleteAndSave(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().RemoveRange(entities);
        _context.SaveChanges();
    }

    public void DeleteAndSave(Expression<Func<TEntity, bool>> where)
    {
        var entitiesToDelete = _context.Set<TEntity>().Where(where);
        _context.Set<TEntity>().RemoveRange(entitiesToDelete);
        _context.SaveChanges();
    }
    #endregion

    #region IsExist & Count
    public bool IsExist(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (ignoreFilters) query = query.IgnoreQueryFilters();

        return query.Any();
    }

    public int Count(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (ignoreFilters) query = query.IgnoreQueryFilters();

        return query.Count();
    }
    #endregion

    #region Get
    public TEntity? Get(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return query.SingleOrDefault();
    }

    public TResult? Get<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false)
    {
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return query.Select(select).SingleOrDefault();
    }

    public TResult? Get<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    )
    {
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return query.ProjectTo<TResult>(configurationProvider).SingleOrDefault();
    }
    #endregion

    #region GetAll
    public ICollection<TEntity>? GetAll(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return query.ToList();
    }

    public ICollection<TResult>? GetAll<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false)
    {
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return query.Select(select).ToList();
    }

    public ICollection<TResult>? GetAll<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false)
    {
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return query.ProjectTo<TResult>(configurationProvider).ToList();
    }
    #endregion

    #region Datatable Server-Side
    public DatatableResponseServerSide<TEntity> DatatableServerSide(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        if (datatableRequest == null) throw new ArgumentNullException(nameof(datatableRequest));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.ToDatatableServerSide(datatableRequest);
    }

    public DatatableResponseServerSide<TResult> DatatableServerSide<TResult>(
        DatatableRequest datatableRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        if (datatableRequest == null) throw new ArgumentNullException(nameof(datatableRequest));
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.Select(select).ToDatatableServerSide(datatableRequest);
    }

    public DatatableResponseServerSide<TResult> DatatableServerSide<TResult>(
        DatatableRequest datatableRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        if (datatableRequest == null) throw new ArgumentNullException(nameof(datatableRequest));
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.ProjectTo<TResult>(configurationProvider).ToDatatableServerSide(datatableRequest);
    }
    #endregion

    #region Datatable Client-Side
    public DatatableResponseClientSide<TEntity> DatatableClientSide(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.ToDatatableClientSide();
    }

    public DatatableResponseClientSide<TResult> DatatableClientSide<TResult>(
      Expression<Func<TEntity, TResult>> select,
      Filter? filter = null,
      IEnumerable<Sort>? sorts = null,
      Expression<Func<TEntity, bool>>? where = null,
      Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
      Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
      bool ignoreFilters = false)
    {
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.Select(select).ToDatatableClientSide();
    }

    public DatatableResponseClientSide<TResult> DatatableClientSide<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.ProjectTo<TResult>(configurationProvider).ToDatatableClientSide();
    }
    #endregion

    #region Pagination
    public PaginationResponse<TEntity> Pagination(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        if (paginationRequest == null) throw new ArgumentNullException(nameof(paginationRequest));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.ToPaginate(paginationRequest);
    }

    public PaginationResponse<TResult> Pagination<TResult>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        if (paginationRequest == null) throw new ArgumentNullException(nameof(paginationRequest));
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.Select(select).ToPaginate(paginationRequest);
    }

    public PaginationResponse<TResult> Pagination<TResult>(
        PaginationRequest paginationRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        if (paginationRequest == null) throw new ArgumentNullException(nameof(paginationRequest));
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return query.ProjectTo<TResult>(configurationProvider).ToPaginate(paginationRequest);
    }
    #endregion


    // ############################# Async Methods #############################
    #region Add
    public async Task<TEntity> AddAndSaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<List<TEntity>> AddAndSaveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
        return entities.ToList();
    }
    #endregion

    #region Update
    public async Task<TEntity> UpdateAndSaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<List<TEntity>> UpdateAndSaveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
        return entities.ToList();
    }
    #endregion

    #region Delete
    public async Task DeleteAndSaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAndSaveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAndSaveAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default)
    {
        var entitiesToDelete = _context.Set<TEntity>().Where(where);
        _context.Set<TEntity>().RemoveRange(entitiesToDelete);
        await _context.SaveChangesAsync(cancellationToken);
    }
    #endregion

    #region IsExist & Count
    public async Task<bool> IsExistAsync(
        Filter? filter = null,
        Expression<Func<TEntity, bool>>? where = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (ignoreFilters) query = query.IgnoreQueryFilters();

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        Filter? filter = null,
        Expression<Func<TEntity, bool>>? where = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (ignoreFilters) query = query.IgnoreQueryFilters();

        return await query.CountAsync(cancellationToken);
    }
    #endregion

    #region Get
    public async Task<TEntity?> GetAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return await query.Select(select).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TResult?> GetAsync<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default
    )
    {
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return await query.ProjectTo<TResult>(configurationProvider).SingleOrDefaultAsync(cancellationToken);
    }
    #endregion

    #region GetAll
    public async Task<ICollection<TEntity>?> GetAllAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ICollection<TResult>?> GetAllAsync<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return await query.Select(select).ToListAsync(cancellationToken);
    }

    public async Task<ICollection<TResult>?> GetAllAsync<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default
    )
    {
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        if (!tracking) query = query.AsNoTracking();

        return await query.ProjectTo<TResult>(configurationProvider).ToListAsync(cancellationToken);
    }
    #endregion

    #region Datatable Server-Side
    public async Task<DatatableResponseServerSide<TEntity>> DatatableServerSideAsync(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (datatableRequest == null) throw new ArgumentNullException(nameof(datatableRequest));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.ToDatatableServerSideAsync(datatableRequest, cancellationToken);
    }

    public async Task<DatatableResponseServerSide<TResult>> DatatableServerSideAsync<TResult>(
        DatatableRequest datatableRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (datatableRequest == null) throw new ArgumentNullException(nameof(datatableRequest));
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.Select(select).ToDatatableServerSideAsync(datatableRequest, cancellationToken);
    }

    public async Task<DatatableResponseServerSide<TResult>> DatatableServerSideAsync<TResult>(
        DatatableRequest datatableRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (datatableRequest == null) throw new ArgumentNullException(nameof(datatableRequest));
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.ProjectTo<TResult>(configurationProvider).ToDatatableServerSideAsync(datatableRequest, cancellationToken);
    }
    #endregion

    #region Datatable Client-Side
    public async Task<DatatableResponseClientSide<TEntity>> DatatableClientSideAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.ToDatatableClientSideAsync(cancellationToken);
    }

    public async Task<DatatableResponseClientSide<TResult>> DatatableClientSideAsync<TResult>(
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.Select(select).ToDatatableClientSideAsync(cancellationToken);
    }

    public async Task<DatatableResponseClientSide<TResult>> DatatableClientSideAsync<TResult>(
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.ProjectTo<TResult>(configurationProvider).ToDatatableClientSideAsync(cancellationToken);
    }
    #endregion

    #region Pagination
    public async Task<PaginationResponse<TEntity>> PaginationAsync(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (paginationRequest == null) throw new ArgumentNullException(nameof(paginationRequest));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.ToPaginateAsync(paginationRequest, cancellationToken);
    }

    public async Task<PaginationResponse<TResult>> PaginationAsync<TResult>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TResult>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (paginationRequest == null) throw new ArgumentNullException(nameof(paginationRequest));
        if (select == null) throw new ArgumentNullException(nameof(select));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.Select(select).ToPaginateAsync(paginationRequest, cancellationToken);
    }

    public async Task<PaginationResponse<TResult>> PaginationAsync<TResult>(
        PaginationRequest paginationRequest,
        IConfigurationProvider configurationProvider,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        if (paginationRequest == null) throw new ArgumentNullException(nameof(paginationRequest));
        if (configurationProvider == null) throw new ArgumentNullException(nameof(configurationProvider));

        var query = _context.Set<TEntity>().AsQueryable();

        if (where != null) query = query.Where(where);
        if (filter != null) query = query.ToFilter(filter);
        if (orderBy != null) query = orderBy(query);
        if (sorts != null) query = query.ToSort(sorts);
        if (include != null) query = include(query);
        if (ignoreFilters) query = query.IgnoreQueryFilters();
        query = query.AsNoTracking();

        return await query.ProjectTo<TResult>(configurationProvider).ToPaginateAsync(paginationRequest, cancellationToken);
    }
    #endregion
}";

        string folderPath = Path.Combine(solutionPath, "DataAccess", "Repository");

        var results = new List<string>
        {
            AddFile(folderPath, "IRepository", code_IRepository),
            AddFile(folderPath, "IRepositoryAsync", code_IRepositoryAsync),
            AddFile(folderPath, "RepositoryBase", code_RepositoryBase_1 + contextRule + code_RepositoryBase_2)
        };

        return string.Join("\n", results);
    }

    public string GenerateInterceptors(string solutionPath)
    {
        string code_ArchiveInterceptor = @"using Core.Model;
using Core.Utils.HttpContextManager;
using DataAccess.Interceptors.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Model.ProjectEntities;

namespace DataAccess.Interceptors;

public sealed class ArchiveInterceptor : SaveChangesInterceptor
{
    private readonly HttpContextManager _httpContextManager;
    public ArchiveInterceptor(HttpContextManager httpContextManager) => _httpContextManager = httpContextManager;


    //  ****************************** SYNC VERSION ******************************
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        IEnumerable<EntityEntry<IArchivableEntity>> archivableEntries = eventData.Context.ChangeTracker.Entries<IArchivableEntity>()
            .Where(e => (e.State == EntityState.Modified || e.State == EntityState.Deleted) && e.Entity is not IProjectEntity);

        if (archivableEntries.Any())
        {
            List<Archive> archives = new List<Archive>();
            foreach (EntityEntry<IArchivableEntity> entry in archivableEntries)
            {
                archives.Add(new Archive
                {
                    TableName = entry.GetTableName(),
                    EntityId = entry.GetEntityId(),
                    RequesterId = _httpContextManager.GetUserId(),
                    ClientIp = _httpContextManager.GetClientIp(),
                    UserAgent = _httpContextManager.GetUserAgent(),
                    Action = entry.GetActionType(),
                    DateUtc = DateTime.UtcNow,
                    Data = entry.GetOriginalData(),
                });
            }
            eventData.Context.Set<Archive>().AddRange(archives);
        }

        return base.SavingChanges(eventData, result);
    }


    //  ****************************** ASYNC VERSION ******************************
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<IArchivableEntity>> archivableEntries = eventData.Context.ChangeTracker.Entries<IArchivableEntity>()
            .Where(e => (e.State == EntityState.Modified || e.State == EntityState.Deleted) && e.Entity is not IProjectEntity);

        if (archivableEntries.Any())
        {
            List<Archive> archives = new List<Archive>();
            foreach (EntityEntry<IArchivableEntity> entry in archivableEntries)
            {
                archives.Add(new Archive
                {
                    TableName = entry.GetTableName(),
                    EntityId = entry.GetEntityId(),
                    RequesterId = _httpContextManager.GetUserId(),
                    ClientIp = _httpContextManager.GetClientIp(),
                    UserAgent = _httpContextManager.GetUserAgent(),
                    Action = entry.GetActionType(),
                    DateUtc = DateTime.UtcNow,
                    Data = entry.GetOriginalData(),
                });
            }
            eventData.Context.Set<Archive>().AddRange(archives);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}";

        string code_AuditInterceptor = @"using Core.Model;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Core.Utils.HttpContextManager;

namespace DataAccess.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly HttpContextManager _httpContextManager;
    public AuditInterceptor(HttpContextManager httpContextManager) => _httpContextManager = httpContextManager;


    //  ****************************** SYNC VERSION ******************************
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        IEnumerable<EntityEntry<IAuditableEntity>> auditableEntries = eventData.Context.ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && e.Entity is not IProjectEntity);

        if (auditableEntries.Any())
        {
            foreach (EntityEntry<IAuditableEntity> entry in auditableEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = _httpContextManager.GetUserId();
                    entry.Entity.CreateDateUtc = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedBy = _httpContextManager.GetUserId();
                    entry.Entity.UpdateDateUtc = DateTime.UtcNow;
                }
            }
        }

        return base.SavingChanges(eventData, result);
    }


    //  ****************************** ASYNC VERSION ******************************
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<IAuditableEntity>> auditableEntries = eventData.Context.ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && e.Entity is not IProjectEntity);

        if (auditableEntries.Any())
        {
            foreach (EntityEntry<IAuditableEntity> entry in auditableEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = _httpContextManager.GetUserId();
                    entry.Entity.CreateDateUtc = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedBy = _httpContextManager.GetUserId();
                    entry.Entity.UpdateDateUtc = DateTime.UtcNow;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}";

        string code_LogInterceptor = @"using Core.Model;
using Core.Utils.HttpContextManager;
using DataAccess.Interceptors.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Model.ProjectEntities;

namespace DataAccess.Interceptors;

public sealed class LogInterceptor : SaveChangesInterceptor
{
    private readonly HttpContextManager _httpContextManager;
    private readonly List<(Log LogEntry, EntityEntry EntityEntry)> _pendingLogs = new();
    public LogInterceptor(HttpContextManager httpContextManager) => _httpContextManager = httpContextManager;


    //  ****************************** SYNC VERSION ******************************
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        IEnumerable<EntityEntry<ILoggableEntity>> loggableEntries = eventData.Context.ChangeTracker.Entries<ILoggableEntity>()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted) && e.Entity is not IProjectEntity);

        if (loggableEntries.Any())
        {
            List<Log> logsToInsert = new List<Log>();
            foreach (EntityEntry<ILoggableEntity> entry in loggableEntries)
            {
                Log log = new Log
                {
                    TableName = entry.GetTableName(),
                    RequesterId = _httpContextManager.GetUserId(),
                    ClientIp = _httpContextManager.GetClientIp(),
                    UserAgent = _httpContextManager.GetUserAgent(),
                    Action = entry.GetActionType(),
                    DateUtc = DateTime.UtcNow,
                };

                if (entry.State == EntityState.Added)
                {
                    _pendingLogs.Add((log, entry));
                }
                else if (entry.State == EntityState.Deleted)
                {
                    log.EntityId = entry.GetEntityId();
                    log.Data = entry.GetOriginalData();

                    logsToInsert.Add(log);
                }
                else if (entry.State == EntityState.Modified)
                {
                    log.EntityId = entry.GetEntityId();
                    log.OldData = entry.GetOriginalData();
                    log.NewData = entry.GetCurrentData();

                    logsToInsert.Add(log);
                }
            }
            eventData.Context.Set<Log>().AddRange(logsToInsert);
        }

        return base.SavingChanges(eventData, result);
    }


    //  ****************************** ASYNC VERSION ******************************
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<ILoggableEntity>> loggableEntries = eventData.Context.ChangeTracker.Entries<ILoggableEntity>()
            .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted) && e.Entity is not IProjectEntity);

        if (loggableEntries.Any())
        {
            List<Log> logsToInsert = new List<Log>();
            foreach (EntityEntry<ILoggableEntity> entry in loggableEntries)
            {
                Log log = new Log
                {
                    TableName = entry.GetTableName(),
                    RequesterId = _httpContextManager.GetUserId(),
                    ClientIp = _httpContextManager.GetClientIp(),
                    UserAgent = _httpContextManager.GetUserAgent(),
                    Action = entry.GetActionType(),
                    DateUtc = DateTime.UtcNow,
                };

                if (entry.State == EntityState.Added)
                {
                    _pendingLogs.Add((log, entry));
                }
                else if (entry.State == EntityState.Deleted)
                {
                    log.EntityId = entry.GetEntityId();
                    log.Data = entry.GetOriginalData();

                    logsToInsert.Add(log);
                }
                else if (entry.State == EntityState.Modified)
                {
                    log.EntityId = entry.GetEntityId();
                    log.OldData = entry.GetOriginalData();
                    log.NewData = entry.GetCurrentData();

                    logsToInsert.Add(log);
                }
            }
            eventData.Context.Set<Log>().AddRange(logsToInsert);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }




    //  ****************************** SYNC VERSION ******************************
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is null) return base.SavedChanges(eventData, result);

        if (_pendingLogs.Any())
        {
            foreach (var (log, entityEntry) in _pendingLogs)
            {
                log.EntityId = entityEntry.GetEntityId();
                log.Data = entityEntry.GetCurrentData();
            }

            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = false;
            eventData.Context.Set<Log>().AddRange(_pendingLogs.Select(p => p.LogEntry));
            eventData.Context.SaveChanges();
            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = true;

            _pendingLogs.Clear();
        }

        return base.SavedChanges(eventData, result);
    }

    //  ****************************** ASYNC VERSION ******************************
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavedChangesAsync(eventData, result, cancellationToken);

        if (_pendingLogs.Any())
        {
            foreach (var (log, entityEntry) in _pendingLogs)
            {
                log.EntityId = entityEntry.GetEntityId();
                log.Data = entityEntry.GetCurrentData();
            }

            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = false;
            eventData.Context.Set<Log>().AddRange(_pendingLogs.Select(p => p.LogEntry));
            _pendingLogs.Clear();
            eventData.Context.SaveChanges();
            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}";

        string code_SoftDeleteInterceptor = @"using Core.Model;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Core.Utils.HttpContextManager;

namespace DataAccess.Interceptors;

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly HttpContextManager _httpContextManager;
    public SoftDeleteInterceptor(HttpContextManager httpContextManager) => _httpContextManager = httpContextManager;


    //  ****************************** SYNC VERSION ******************************
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        IEnumerable<EntityEntry<ISoftDeletableEntity>> softDeletableEntries = eventData.Context.ChangeTracker.Entries<ISoftDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted && e.Entity is not IProjectEntity);

        if (softDeletableEntries.Any())
        {
            foreach (EntityEntry<ISoftDeletableEntity> entry in softDeletableEntries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedBy = _httpContextManager.GetUserId();
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedDateUtc = DateTime.UtcNow;
            }
        }

        return base.SavingChanges(eventData, result);
    }


    //  ****************************** ASYNC VERSION ******************************
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<ISoftDeletableEntity>> softDeletableEntries = eventData.Context.ChangeTracker.Entries<ISoftDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted && e.Entity is not IProjectEntity);

        if (softDeletableEntries.Any())
        {
            foreach (EntityEntry<ISoftDeletableEntity> entry in softDeletableEntries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedBy = _httpContextManager.GetUserId();
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedDateUtc = DateTime.UtcNow;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}";

        string code_Helpers = @"using Core.Enums;
using Core.Utils.CriticalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;

namespace DataAccess.Interceptors.Helpers;

public static class EntityEntryExtension
{
    public static string? GetTableName(this EntityEntry entry)
    {
        string? tableName = default;
        if (entry.Entity != null)
        {
            tableName = entry.Entity.GetType().Name;
        }
        return tableName;
    }

    public static string? GetEntityId(this EntityEntry entry)
    {
        var primaryKeys = entry.Metadata.FindPrimaryKey()?.Properties.Select(pk => entry.Property(pk.Name).CurrentValue?.ToString()).Where(v => !string.IsNullOrEmpty(v));
        string? entityId = default;
        if (primaryKeys != null && primaryKeys.Count() > 0)
        {
            if (primaryKeys.Count() == 1) entityId = primaryKeys.FirstOrDefault();
            else entityId = primaryKeys.OrderByDescending(x => x).Aggregate((a, b) => $""{a}-{b}"");
        }
        return entityId;
    }

    public static CrudTypes GetActionType(this EntityEntry entry)
    {
        CrudTypes actionType = CrudTypes.Undefined;
        if (entry.State == EntityState.Added)
        {
            actionType = CrudTypes.Create;
        }
        else if (entry.State == EntityState.Modified)
        {
            actionType = CrudTypes.Update;
        }
        else if (entry.State == EntityState.Deleted)
        {
            actionType = CrudTypes.Delete;
        }
        return actionType;
    }

    public static string? GetOriginalData(this EntityEntry entry)
    {
        string? data = string.Empty;
        if (entry.OriginalValues != null)
        {
            data = JsonConvert.SerializeObject(entry.OriginalValues.ToObject(), new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = 7,
                ContractResolver = new IgnoreCriticalDataResolver()
            });
        }
        return data;
    }

    public static string? GetCurrentData(this EntityEntry entry)
    {
        string? data = string.Empty;
        if (entry.Entity != null)
        {
            data = JsonConvert.SerializeObject(entry.CurrentValues.ToObject(), new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = 7,
                ContractResolver = new IgnoreCriticalDataResolver()
            });
        }
        return data;
    }
}";

        string folderPath = Path.Combine(solutionPath, "DataAccess", "Interceptors");
        string folderPathHelpser = Path.Combine(solutionPath, "DataAccess", "Interceptors", "Helpers");

        var results = new List<string>
        {
            AddFile(folderPath, "ArchiveInterceptor", code_ArchiveInterceptor),
            AddFile(folderPath, "AuditInterceptor", code_AuditInterceptor),
            AddFile(folderPath, "LogInterceptor", code_LogInterceptor),
            AddFile(folderPath, "SoftDeleteInterceptor", code_SoftDeleteInterceptor),
            AddFile(folderPathHelpser, "EntityEntryExtension", code_Helpers)
        };

        return string.Join("\n", results);
    }
    #endregion

    public string GenerateServices(string solutionPath)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        var roslynRepositoryServiceGenerator = new RoslynRepositoryServiceGenerator();

        string folderPathAbstract = Path.Combine(solutionPath, "DataAccess", "Abstract");
        string folderPathConcrete = Path.Combine(solutionPath, "DataAccess", "Concrete");

        foreach (var entity in entities)
        {
            string code_abstract = roslynRepositoryServiceGenerator.GeneraterAbstract(entity.Name);
            string code_concrete = roslynRepositoryServiceGenerator.GeneraterConcrete(entity.Name);

            results.Add(AddFile(folderPathAbstract, $"I{entity.Name}Repository", code_abstract));
            results.Add(AddFile(folderPathConcrete, $"{entity.Name}Repository", code_concrete));
        }


        if (_appSetting.IsThereIdentiy)
        {
            string code_abstract = roslynRepositoryServiceGenerator.GeneraterAbstract("RefreshToken");
            string code_concrete = roslynRepositoryServiceGenerator.GeneraterConcrete("RefreshToken");

            results.Add(AddFile(folderPathAbstract, "IRefreshTokenRepository", code_abstract));
            results.Add(AddFile(folderPathConcrete, "RefreshTokenRepository", code_concrete));
        }

        return string.Join("\n", results);
    }

    public string GenerateUOW(string solutionPath)
    {
        var results = new List<string>();

        var roslynUOWGenerator = new RoslynUOWGenerator(_appSetting);

        var entities = _entityRepository.GetAll(f => f.Control == false);

        string code_IUnitOfWork = roslynUOWGenerator.GeneraterAbstract(entities);
        string code_UnitOfWork = roslynUOWGenerator.GeneraterConcrete(entities);

        string folderPath = Path.Combine(solutionPath, "DataAccess", "UoW");

        results.Add(AddFile(folderPath, "IUnitOfWork", code_IUnitOfWork));
        results.Add(AddFile(folderPath, "UnitOfWork", code_UnitOfWork));

        return string.Join("\n", results);
    }

    public string GenerateContext(string solutionPath)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields));

        var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);
        string IdentityKeyType = identityTypeConfigs.IdentityKeyType;
        string IdentityUserType = identityTypeConfigs.IdentityUserType;
        string IdentityRoleType = identityTypeConfigs.IdentityRoleType;


        // Concretes
        StringBuilder sb = new();
        sb.AppendLine("using Microsoft.AspNetCore.Identity;");
        sb.AppendLine("using Microsoft.AspNetCore.Identity.EntityFrameworkCore;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Model.Entities;");
        sb.AppendLine("using Model.ProjectEntities;");
        sb.AppendLine();
        sb.AppendLine("namespace DataAccess.Contexts;");
        sb.AppendLine();
        if (_appSetting.IsThereIdentiy)
        {
            sb.AppendLine($"public class AppDbContext : IdentityDbContext<{IdentityUserType}, {IdentityRoleType}, {IdentityKeyType}> // DbContext");
        }
        else
        {
            sb.AppendLine($"public class AppDbContext : DbContext");
        }
        sb.AppendLine("{");
        sb.AppendLine("\tpublic AppDbContext(DbContextOptions<AppDbContext> options) : base(options)");
        sb.AppendLine("\t{");
        sb.AppendLine("\t}");
        sb.AppendLine();
        foreach (var entity in entities)
        {
            if ((_appSetting.IsThereUser && entity.Id == _appSetting.UserEntityId) || (_appSetting.IsThereRole && entity.Id == _appSetting.RoleEntityId))
            {
                sb.AppendLine($"\tpublic override DbSet<{entity.Name}> {entity.Name.Pluralize()} {{ get; set; }}");
            }
            else
            {
                sb.AppendLine($"\tpublic DbSet<{entity.Name}> {entity.Name.Pluralize()} {{ get; set; }}");
            }
        }
        if (_appSetting.IsThereIdentiy)
        {
            sb.AppendLine($"\tpublic DbSet<RefreshToken> RefreshTokens {{ get; set; }}");
        }

        sb.AppendLine();
        sb.AppendLine("\t// Project Entities");
        sb.AppendLine("\tpublic DbSet<Log> Logs { get; set; }");
        sb.AppendLine("\tpublic DbSet<Archive> Archives { get; set; }");
        sb.AppendLine("\t// Project Entities");
        sb.AppendLine();
        sb.AppendLine("\tprotected override void OnModelCreating(ModelBuilder modelBuilder)");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tbase.OnModelCreating(modelBuilder);");
        #region OnModelCreating
        sb.AppendLine();

        // Entity Fluent Codes
        foreach (var entity in entities)
        {
            char eSc = entity.Name.Trim().ToLowerInvariant()[0];
            sb.AppendLine($"\t\tmodelBuilder.Entity<{entity.Name}>({eSc} =>");
            sb.AppendLine("\t\t{");
            if (!string.IsNullOrEmpty(entity.TableName))
            {
                sb.AppendLine($"\t\t\t{eSc}.ToTable(\"{entity.TableName}\");");
                sb.AppendLine();
            }
            // **** HasKey ****
            if (entity.Fields.Count(f => f.IsUnique) == 1)
            {
                var uniqueField = entity.Fields.First(f => f.IsUnique);
                sb.AppendLine($"\t\t\t{eSc}.HasKey({eSc} => {eSc}.{uniqueField.Name});");
            }
            else
            {
                var uniqueFields = entity.Fields.Where(f => f.IsUnique).Select(x => $"{eSc}.{x.Name}");
                string keys = string.Join(",", uniqueFields);
                sb.AppendLine($"\t\t\t{eSc}.HasKey({eSc} => new {{ {keys} }});");
            }
            // **** HasKey ****

            sb.AppendLine();

            // **** Relations ****
            var relations = _relationRepository.GetRelationsOfEntity(entity.Id);
            foreach (var relation in relations)
            {
                char f_eSc = relation.ForeignField.Entity.Name.Trim().ToLowerInvariant()[0];
                if (relation.RelationTypeId == (int)RelationTypeEnums.OneToOne)
                {
                    if (relation.PrimaryField.EntityId == entity.Id)
                    {
                        sb.AppendLine($"\t\t\t{eSc}.HasOne({eSc} => {eSc}.{relation.PrimaryEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.WithOne({f_eSc} => {f_eSc}.{relation.ForeignEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.HasForeignKey<{relation.ForeignField.Entity.Name}>({f_eSc} => {f_eSc}.{relation.ForeignField.Name})");
                        sb.AppendLine($"\t\t\t\t.OnDelete({relation.GetOnDeleteType()});");
                    }
                    else
                    {
                        sb.AppendLine($"\t\t\t{eSc}.HasOne({eSc} => {eSc}.{relation.ForeignEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.WithOne({f_eSc} => {f_eSc}.{relation.PrimaryEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.HasForeignKey<{relation.PrimaryField.Entity.Name}>({f_eSc} => {f_eSc}.{relation.PrimaryField.Name})");
                        sb.AppendLine($"\t\t\t\t.OnDelete({relation.GetOnDeleteType()});");
                    }
                }
                else if (relation.RelationTypeId == (int)RelationTypeEnums.OneToMany)
                {
                    if (relation.PrimaryField.EntityId == entity.Id)
                    {
                        sb.AppendLine($"\t\t\t{eSc}.HasMany({eSc} => {eSc}.{relation.PrimaryEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.WithOne({f_eSc} => {f_eSc}.{relation.ForeignEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.HasForeignKey({f_eSc} => {f_eSc}.{relation.ForeignField.Name})");
                        sb.AppendLine($"\t\t\t\t.OnDelete({relation.GetOnDeleteType()});");
                    }
                    else
                    {
                        sb.AppendLine($"\t\t\t{eSc}.HasOne({eSc} => {eSc}.{relation.ForeignEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.WithMany({f_eSc} => {f_eSc}.{relation.PrimaryEntityVirPropName})");
                        sb.AppendLine($"\t\t\t\t.HasForeignKey({eSc} => {eSc}.{relation.ForeignField.Name})");
                        sb.AppendLine($"\t\t\t\t.OnDelete({relation.GetOnDeleteType()});");
                    }
                }
                sb.AppendLine();
            }
            // **** Relations ****

            if (_appSetting.IsThereUser && entity.Id == _appSetting.UserEntityId)
            {
                sb.AppendLine(@"
    u.HasMany(u => u.RefreshTokens)
        .WithOne(r => r.User)
        .HasForeignKey(r => r.UserId)
        .OnDelete(DeleteBehavior.Cascade);
");
            }

            // **** Seed Data ****
            if (_appSetting.IsThereRole && entity.Id == _appSetting.RoleEntityId)
            {
                var userId = new Guid();
                var managerId = new Guid();
                var adminId = new Guid();
                var ownerId = new Guid();

                sb.AppendLine(@$"           
            {eSc}.HasData(
                new
                {{
                    Id = {userId},
                    Name = ""User"",
                    NormalizedName = ""USER"",
                    ConcurrencyStamp = ""{userId.ToString()}""
                }},
                new
                {{
                    Id = {managerId},
                    Name = ""Manager"",
                    NormalizedName = ""MANAGER"",
                    ConcurrencyStamp = ""{managerId.ToString()}""
                }},
                new
                {{
                    Id = {adminId},
                    Name = ""Admin"",
                    NormalizedName = ""ADMIN"",
                    ConcurrencyStamp = ""{adminId.ToString()}""
                }},
                new
                {{
                    Id = {ownerId},
                    Name = ""Owner"",
                    NormalizedName = ""OWNER"",
                    ConcurrencyStamp = ""{ownerId.ToString()}""
                }}
            );");
            }
            // **** Seed Data ****

            if (entity.SoftDeletable)
            {
                sb.AppendLine($"\t\t\t{eSc}.HasQueryFilter(f => !f.IsDeleted);");
            }
            sb.AppendLine("\t\t});");
            sb.AppendLine();
        }

        // RefreshToken Fleunt Codes
        if (_appSetting.IsThereIdentiy)
        {
            sb.AppendLine(@"        
        modelBuilder.Entity<RefreshToken>(r =>
        {
            r.HasKey(r => r.Id);

            r.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });");
        }
        sb.AppendLine();

        // Project Entites Fleunt Codes
        sb.AppendLine(@"
        modelBuilder.Entity<Log>(l =>
        {
            l.ToTable(""ProjectLogs"");

            l.HasKey(l => l.Id);
        });

        modelBuilder.Entity<Archive>(a =>
        {
            a.ToTable(""ProjectArchives"");

            a.HasKey(a => a.Id);
        });");

        sb.AppendLine();

        if (_appSetting.IsThereIdentiy)
        {
            if (!_appSetting.IsThereUser)
            {
                sb.AppendLine($"\t\tmodelBuilder.Entity<IdentityUser<{IdentityKeyType}>>(entity => {{ entity.ToTable(\\\"Users\\\"); }});");
            }

            if (!_appSetting.IsThereRole)
            {
                Dictionary<string, string> defaultRoles = new Dictionary<string, string>
                {
                    { 
                        "User", 
                        IdentityKeyType == "int" ? "1" :
                            IdentityKeyType == "string" ? "b370875e-34cd-4b79-891c-93ae38f99d11" :
                            "new Guid(\"b370875e-34cd-4b79-891c-93ae38f99d11\")"
                    },
                    { 
                        "Manager", 
                        IdentityKeyType == "int" ? "2" :
                            IdentityKeyType == "string" ? "cd6040ef-dacc-4678-9a85-154f12581cff" :
                                "new Guid(\"cd6040ef-dacc-4678-9a85-154f12581cff\")"
                    },
                    { 
                        "Admin", 
                        IdentityKeyType == "int" ? "3" :
                            IdentityKeyType == "string" ? "7138ec51-4f9e-4afd-b61b-5a9a4584f5da" :
                                "new Guid(\"7138ec51-4f9e-4afd-b61b-5a9a4584f5da\")"
                    },
                    { 
                        "Owner", 
                        IdentityKeyType == "int" ? "4" : 
                            IdentityKeyType == "string" ? "1f20c152-530e-4064-a39c-bbbed341fe84" :
                                "new Guid(\"1f20c152-530e-4064-a39c-bbbed341fe84\")"
                    }
                };
                sb.AppendLine($"\t\tmodelBuilder.Entity<IdentityRole<{IdentityKeyType}>>(entity =>");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tentity.ToTable(\"Roles\");");
                sb.AppendLine();
                sb.AppendLine("\t\t\tentity.HasData(");
                var _indexOfRole = 1;
                foreach (var roleData in defaultRoles)
                {
                    sb.AppendLine("\t\t\t\tnew");
                    sb.AppendLine("\t\t\t\t{");
                    sb.AppendLine($"\t\t\t\t\tId = {roleData.Value}," );
                    sb.AppendLine($"\t\t\t\t\tName = \"{roleData.Key}\"," );
                    sb.AppendLine($"\t\t\t\t\tNormalizedName = \"{roleData.Key.ToUpperInvariant()}\"," );
                    sb.AppendLine($"\t\t\t\t\tConcurrencyStamp= {roleData.Value}" );
                    if (_indexOfRole == defaultRoles.Count) 
                        sb.AppendLine("\t\t\t\t}");
                    else 
                        sb.AppendLine("\t\t\t\t},");
                    _indexOfRole++;
                } 
                sb.AppendLine("\t\t\t);");
                sb.AppendLine("\t\t});"); 

            }
            sb.AppendLine($"\t\tmodelBuilder.Entity<IdentityUserClaim<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserClaims\"); }});\n");
            sb.AppendLine($"\t\tmodelBuilder.Entity<IdentityUserLogin<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserLogins\"); }});\n");
            sb.AppendLine($"\t\tmodelBuilder.Entity<IdentityRoleClaim<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"RoleClaims\"); }});\n");
            sb.AppendLine($"\t\tmodelBuilder.Entity<IdentityUserRole<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserRoles\"); }});\n");
            sb.AppendLine($"\t\tmodelBuilder.Entity<IdentityUserToken<{IdentityKeyType}>>(entity => {{ entity.ToTable(\"UserTokens\"); }});\n");
        }

        #endregion
        sb.AppendLine("\t}");
        sb.AppendLine("}");

        string folderPath = Path.Combine(solutionPath, "DataAccess", "Contexts");
        results.Add(AddFile(folderPath, "AppDbContext", sb.ToString()));

        return string.Join("\n", results);
    }

    public string GenerateServiceRegistrations(string solutionPath)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        StringBuilder sb = new();
        sb.AppendLine("using Autofac;");
        sb.AppendLine("using Autofac.Extras.DynamicProxy;");
        sb.AppendLine("using Core.Utils.CrossCuttingConcerns;");
        sb.AppendLine("using DataAccess.Abstract;");
        sb.AppendLine("using DataAccess.Concrete;");
        sb.AppendLine();
        sb.AppendLine("namespace DataAccess;");
        sb.AppendLine();
        sb.AppendLine("public class AutofacModule : Module");
        sb.AppendLine("{");
        sb.AppendLine("\tprotected override void Load(ContainerBuilder builder)");
        sb.AppendLine("\t{");
        foreach (var entity in entities)
        {
            sb.AppendLine($"\t\tbuilder.RegisterType<{entity.Name}Repository>().As<I{entity.Name}Repository>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(DataAccessExceptionHandlerInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
            sb.AppendLine();
        }
        if (_appSetting.IsThereIdentiy)
        {
            sb.AppendLine($"\t\tbuilder.RegisterType<RefreshTokenRepository>().As<IRefreshTokenRepository>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(DataAccessExceptionHandlerInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
        }
        sb.AppendLine("\t}");
        sb.AppendLine("}");

        string code_ServiceRegistration = @"
using DataAccess.Contexts;
using DataAccess.Interceptors;
using DataAccess.UoW;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess;

public static class ServiceRegistration
{
    public static IServiceCollection AddDataAccessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditInterceptor>();
        services.AddSingleton<ArchiveInterceptor>();
        services.AddSingleton<LogInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, opt) =>
        {
            opt.UseSqlServer(configuration.GetConnectionString(""Database""))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>())
                .AddInterceptors(serviceProvider.GetRequiredService<ArchiveInterceptor>())
                .AddInterceptors(serviceProvider.GetRequiredService<LogInterceptor>())
                .AddInterceptors(serviceProvider.GetRequiredService<SoftDeleteInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}";


        string folderPath = Path.Combine(solutionPath, "DataAccess");
        results.Add(AddFile(folderPath, "AutofacModule", sb.ToString()));
        results.Add(AddFile(folderPath, "ServiceRegistration", code_ServiceRegistration));

        return string.Join("\n", results);
    }

    // **************** HEPLERS ****************
    private string AddFile(string folderPath, string fileName, string code)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"{fileName}.cs");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, code);
                return $"OK: File {fileName} added to DataAccess project.";
            }
            else
            {
                return $"INFO: File {fileName} already exists in DataAccess project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file({fileName}) to DataAccess project. \n Details:{ex.Message}");
        }
    }

    private string RemoveFile(string folderPath, string fileName)
    {
        try
        {
            string filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return $"OK: File {fileName} removed from DataAccess project.";
            }
            else
            {
                return $"INFO: File {fileName} does not exist in DataAccess project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing file ({fileName}) from DataAccess project. \n Details: {ex.Message}");
        }
    }

    private string RunCommand(string workingDirectory, string fileName, string arguments)
    {
        var processInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            string output = process!.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process!.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command failed: {error}");
            }
            else
            {
                return output;
            }
        }
    }
}
