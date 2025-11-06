using Azure.Core;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace GeneratorWPF.CodeGenerators.NLayer.Business;

public class NLayerBusinessService
{
    private readonly DtoRepository _dtoRepository;
    private readonly DtoFieldRepository _dtoFieldRepository;
    private readonly DtoFieldRelationsRepository _dtoFieldRelationsRepository;
    private readonly EntityRepository _entityRepository;
    private readonly RelationRepository _relationRepository;
    private readonly FieldRepository _fieldRepository;
    private readonly AppSetting _appSetting;
    public NLayerBusinessService(AppSetting appSetting)
    {
        _appSetting = appSetting;
        _dtoRepository = new();
        _dtoFieldRepository = new();
        _dtoFieldRelationsRepository = new();
        _entityRepository = new();
        _fieldRepository = new();
        _relationRepository = new();
    }

    public string CreateProject(string path, string solutionName)
    {
        try
        {
            string projectPath = Path.Combine(path, "Business");
            string csprojPath = Path.Combine(projectPath, "Business.csproj");

            if (Directory.Exists(projectPath) && File.Exists(csprojPath))
                return "INFO: Business layer project already exists.";

            RunCommand(path, "dotnet", "new classlib -n Business");
            RunCommand(path, "dotnet", $"sln {solutionName}.sln add Business/Business.csproj");
            RunCommand(projectPath, "dotnet", $"dotnet add reference ../DataAccess/DataAccess.csproj");

            RemoveFile(projectPath, "Class1.cs");

            return "OK: Business Project Created Successfully";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while creating the Business project. \n\t Details:{ex.Message}");
        }
    }

    #region Package Methods
    public string AddPackage(string path, string packageName)
    {
        try
        {
            string projectPath = Path.Combine(path, "Business");
            string csprojPath = Path.Combine(projectPath, "Business.csproj");

            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"Business.csproj not found for adding package({packageName}).");

            var doc = XDocument.Load(csprojPath);

            var packageAlreadyAdded = doc.Descendants("PackageReference").Any(p => p.Attribute("Include")?.Value == packageName);

            if (packageAlreadyAdded)
                return $"INFO: Package {packageName} already exists in Business project.";

            RunCommand(projectPath, "dotnet", $"add package {packageName}");

            return $"OK: Package {packageName} added to Business project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding pacgace to Business project. \n\t Details:{ex.Message}");
        }
    }
    public string Restore(string path)
    {
        try
        {
            string projectPath = Path.Combine(path, "Business");

            RunCommand(projectPath, "dotnet", "restore");
            return "OK: Restored Business project.";
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while restoring Business project. \n Details:{ex.Message}");
        }
    }
    #endregion

    #region Static Files
    public string GenerateServiceBase(string solutionPath)
    {
        string code_IServiceBase = @"using Core.Model;
using Core.Utils.Datatable;
using Core.Utils.DynamicQuery;
using Core.Utils.Pagination;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Business.ServiceBase;

public interface IServiceBase<TEntity> where TEntity : class, IEntity
{
    #region Insert
    TEntity _Add(TEntity entity);
    TDtoResponse _Add<TDtoResponse>(TEntity entity) where TDtoResponse : IDto;
    TEntity _Add<TDtoRequest>(TDtoRequest insertModel) where TDtoRequest : IDto;
    TDtoResponse _Add<TDtoRequest, TDtoResponse>(TDtoRequest insertModel) where TDtoRequest : IDto where TDtoResponse : IDto;
    #endregion

    #region AddList
    List<TEntity> _AddList(IEnumerable<TEntity> entityList);
    List<TDtoResponse> _AddList<TDtoResponse>(IEnumerable<TEntity> entityList) where TDtoResponse : IDto;
    List<TEntity> _AddList<TDtoRequest>(IEnumerable<TDtoRequest> insertModelList) where TDtoRequest : IDto;
    List<TDtoResponse> _AddList<TDtoRequest, TDtoResponse>(IEnumerable<TDtoRequest> insertModelList) where TDtoRequest : IDto where TDtoResponse : IDto;
    #endregion

    #region Update
    TEntity _Update(TEntity entity, Expression<Func<TEntity, bool>> where);
    TDtoResponse _Update<TDtoResponse>(TEntity entity, Expression<Func<TEntity, bool>> where) where TDtoResponse : IDto;
    TEntity _Update<TDtoRequest>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where) where TDtoRequest : IDto;
    TDtoResponse _Update<TDtoRequest, TDtoResponse>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where) where TDtoRequest : IDto where TDtoResponse : IDto;
    #endregion

    #region UpdateList
    List<TEntity> _UpdateList(IEnumerable<TEntity> entityList);
    List<TDtoResponse> _UpdateList<TDtoResponse>(IEnumerable<TEntity> entityList) where TDtoResponse : IDto;
    #endregion

    #region Delete
    void _Delete(TEntity entity);
    void _Delete(IEnumerable<TEntity> entities);
    void _Delete(Expression<Func<TEntity, bool>> where);
    TEntity _UndoDelete(Expression<Func<TEntity, bool>> where);
    #endregion

    #region IsExist & Count
    bool _IsExist(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false);
    int _Count(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false);
    #endregion

    #region Get
    TEntity? _Get(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true
    );

    TDtoResponse? _Get<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    ) where TDtoResponse : IDto;

    object? _Get(
        Expression<Func<TEntity, object>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    );

    TDtoResponse? _Get<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false
    ) where TDtoResponse : IDto;
    #endregion

    #region GetList
    ICollection<TEntity>? _GetList(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true);

    ICollection<TDtoResponse>? _GetList<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false) where TDtoResponse : IDto;

    ICollection<object>? _GetList(
        Expression<Func<TEntity, object>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false);

    ICollection<TDtoResponse>? _GetList<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false) where TDtoResponse : IDto;
    #endregion

    #region Datatable Server-Side
    DatatableResponseServerSide<TEntity> _DatatableServerSide(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true);

    DatatableResponseServerSide<TDtoResponse> _DatatableServerSide<TDtoResponse>(
       DatatableRequest datatableRequest,
       Expression<Func<TEntity, TDtoResponse>> select,
       Filter? filter = null,
       IEnumerable<Sort>? sorts = null,
       Expression<Func<TEntity, bool>>? where = null,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
       bool ignoreFilters = true) where TDtoResponse : IDto;

    DatatableResponseServerSide<TDtoResponse> _DatatableServerSide<TDtoResponse>(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true) where TDtoResponse : IDto;
    #endregion

    #region Datatable Client-Side
    DatatableResponseClientSide<TEntity> _DatatableClientSide(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true);

    DatatableResponseClientSide<TDtoResponse> _DatatableClientSide<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true) where TDtoResponse : IDto;

    DatatableResponseClientSide<TDtoResponse> _DatatableClientSide<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true) where TDtoResponse : IDto;
    #endregion

    #region Pagination
    PaginationResponse<TEntity> _Pagination(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false);
    PaginationResponse<TDtoResponse> _Pagination<TDtoResponse>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false) where TDtoResponse : IDto;
    PaginationResponse<TDtoResponse> _Pagination<TDtoResponse>(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false) where TDtoResponse : IDto;
    #endregion
}";

        string code_IServiceBaseAsync = @"using Core.Model;
using Core.Utils.Datatable;
using Core.Utils.DynamicQuery;
using Core.Utils.Pagination;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Business.ServiceBase;

public interface IServiceBaseAsync<TEntity> where TEntity : class, IEntity
{
    #region Insert
    Task<TEntity> _AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TDtoResponse> _AddAsync<TDtoResponse>(TEntity entity, CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    Task<TEntity> _AddAsync<TDtoRequest>(TDtoRequest insertModel, CancellationToken cancellationToken = default) where TDtoRequest : IDto;
    Task<TDtoResponse> _AddAsync<TDtoRequest, TDtoResponse>(TDtoRequest insertModel, CancellationToken cancellationToken = default) where TDtoRequest : IDto where TDtoResponse : IDto;
    #endregion

    #region AddList
    Task<List<TEntity>> _AddListAsync(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default);
    Task<List<TDtoResponse>> _AddListAsync<TDtoResponse>(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    Task<List<TEntity>> _AddListAsync<TDtoRequest>(IEnumerable<TDtoRequest> insertModelList, CancellationToken cancellationToken = default) where TDtoRequest : IDto;
    Task<List<TDtoResponse>> _AddListAsync<TDtoRequest, TDtoResponse>(IEnumerable<TDtoRequest> insertModelList, CancellationToken cancellationToken = default) where TDtoRequest : IDto where TDtoResponse : IDto;
    #endregion

    #region Update
    Task<TEntity> _UpdateAsync(TEntity entity, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default);
    Task<TDtoResponse> _UpdateAsync<TDtoResponse>(TEntity entity, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    Task<TEntity> _UpdateAsync<TDtoRequest>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default) where TDtoRequest : IDto;
    Task<TDtoResponse> _UpdateAsync<TDtoRequest, TDtoResponse>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default) where TDtoRequest : IDto where TDtoResponse : IDto;
    #endregion

    #region UpdateList
    Task<List<TEntity>> _UpdateListAsync(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default);
    Task<List<TDtoResponse>> _UpdateListAsync<TDtoResponse>(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    #endregion

    #region Delete
    Task _DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task _DeleteAsync(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default);
    Task _DeleteAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default);
    Task<TEntity> _UndoDeleteAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default);
    #endregion

    #region IsExist & Count
    Task<bool> _IsExistAsync(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false, CancellationToken cancellationToken = default);
    Task<int> _CountAsync(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false, CancellationToken cancellationToken = default);
    #endregion

    #region Get
    Task<TEntity?> _GetAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default);

    Task<TDtoResponse?> _GetAsync<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;

    Task<object?> _GetAsync(
        Expression<Func<TEntity, object>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default);

    Task<TDtoResponse?> _GetAsync<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    #endregion

    #region GetList
    Task<ICollection<TEntity>?> _GetListAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default);

    Task<ICollection<TDtoResponse>?> _GetListAsync<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;

    Task<ICollection<object>?> _GetListAsync(
        Expression<Func<TEntity, object>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default);

    Task<ICollection<TDtoResponse>?> _GetListAsync<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    #endregion

    #region Datatable Server-Side
    Task<DatatableResponseServerSide<TEntity>> _DatatableServerSideAsync(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default);

    Task<DatatableResponseServerSide<TDtoResponse>> _DatatableServerSideAsync<TDtoResponse>(
       DatatableRequest datatableRequest,
       Expression<Func<TEntity, TDtoResponse>> select,
       Filter? filter = null,
       IEnumerable<Sort>? sorts = null,
       Expression<Func<TEntity, bool>>? where = null,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
       bool ignoreFilters = true,
       CancellationToken cancellationToken = default) where TDtoResponse : IDto;

    Task<DatatableResponseServerSide<TDtoResponse>> _DatatableServerSideAsync<TDtoResponse>(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    #endregion

    #region Datatable Client-Side
    Task<DatatableResponseClientSide<TEntity>> _DatatableClientSideAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default);

    Task<DatatableResponseClientSide<TDtoResponse>> _DatatableClientSideAsync<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;

    Task<DatatableResponseClientSide<TDtoResponse>> _DatatableClientSideAsync<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    #endregion

    #region Pagination
    Task<PaginationResponse<TEntity>> _PaginationAsync(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default);

    Task<PaginationResponse<TDtoResponse>> _PaginationAsync<TDtoResponse>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;

    Task<PaginationResponse<TDtoResponse>> _PaginationAsync<TDtoResponse>(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto;
    #endregion
}";

        string code_ServiceBase = @"using AutoMapper;
using Core.Model;
using Core.Utils.Datatable;
using Core.Utils.DynamicQuery;
using Core.Utils.ExceptionHandle.Exceptions;
using Core.Utils.Pagination;
using DataAccess.Repository;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Business.ServiceBase;

public class ServiceBase<TEntity, TRepository> : IServiceBase<TEntity>, IServiceBaseAsync<TEntity>
    where TEntity : class, IEntity
    where TRepository : IRepository<TEntity>, IRepositoryAsync<TEntity>
{
    private readonly TRepository _repository;
    private readonly IMapper _mapper;
    public ServiceBase(TRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }


    // ############################# Sync Methods #############################
    #region Add
    public TEntity _Add(TEntity entity)
    {
        return _repository.AddAndSave(entity);
    }

    public TDtoResponse _Add<TDtoResponse>(TEntity entity) where TDtoResponse : IDto
    {
        TEntity insertedEntity = _repository.AddAndSave(entity);
        return _mapper.Map<TDtoResponse>(insertedEntity);
    }

    public TEntity _Add<TDtoRequest>(TDtoRequest insertModel) where TDtoRequest : IDto
    {
        TEntity mappedEntity = _mapper.Map<TEntity>(insertModel);
        return _repository.AddAndSave(mappedEntity);
    }

    public TDtoResponse _Add<TDtoRequest, TDtoResponse>(TDtoRequest insertModel) where TDtoRequest : IDto where TDtoResponse : IDto
    {
        TEntity mappedEntity = _mapper.Map<TEntity>(insertModel);
        TEntity insertedEntity = _repository.AddAndSave(mappedEntity);
        return _mapper.Map<TDtoResponse>(insertedEntity);
    }
    #endregion

    #region AddList
    public List<TEntity> _AddList(IEnumerable<TEntity> entityList)
    {
        return _repository.AddAndSave(entityList);
    }

    public List<TDtoResponse> _AddList<TDtoResponse>(IEnumerable<TEntity> entityList) where TDtoResponse : IDto
    {
        List<TEntity> insertedEntityList = _repository.AddAndSave(entityList);
        return _mapper.Map<List<TDtoResponse>>(insertedEntityList);
    }

    public List<TEntity> _AddList<TDtoRequest>(IEnumerable<TDtoRequest> insertModelList) where TDtoRequest : IDto
    {
        IEnumerable<TEntity> mappedEntityList = _mapper.Map<IEnumerable<TEntity>>(insertModelList);
        return _repository.AddAndSave(mappedEntityList);
    }

    public List<TDtoResponse> _AddList<TDtoRequest, TDtoResponse>(IEnumerable<TDtoRequest> insertModelList) where TDtoRequest : IDto where TDtoResponse : IDto
    {
        IEnumerable<TEntity> mappedEntityList = _mapper.Map<IEnumerable<TEntity>>(insertModelList);
        List<TEntity> insertedEntityList = _repository.AddAndSave(mappedEntityList);
        return _mapper.Map<List<TDtoResponse>>(insertedEntityList);
    }
    #endregion

    #region Update
    public TEntity _Update(TEntity entity, Expression<Func<TEntity, bool>> where)
    {
        TEntity? originalEntity = _repository.Get(where: where);
        if (originalEntity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        _mapper.Map(entity, originalEntity);

        return _repository.UpdateAndSave(originalEntity);
    }

    public TDtoResponse _Update<TDtoResponse>(TEntity entity, Expression<Func<TEntity, bool>> where) where TDtoResponse : IDto
    {
        TEntity? originalEntity = _repository.Get(where: where);
        if (originalEntity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        _mapper.Map(entity, originalEntity);

        TEntity updatedEntity = _repository.UpdateAndSave(originalEntity);
        return _mapper.Map<TDtoResponse>(updatedEntity);
    }

    public TEntity _Update<TDtoRequest>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where) where TDtoRequest : IDto
    {
        TEntity? entity = _repository.Get(where: where);
        if (entity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        TEntity entityToUpdate = _mapper.Map(updateModel, entity);
        return _repository.UpdateAndSave(entityToUpdate);
    }

    public TDtoResponse _Update<TDtoRequest, TDtoResponse>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where) where TDtoRequest : IDto where TDtoResponse : IDto
    {
        TEntity? entity = _repository.Get(where: where);
        if (entity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        TEntity entityToUpdate = _mapper.Map(updateModel, entity);
        TEntity updatedEntity = _repository.UpdateAndSave(entityToUpdate);
        return _mapper.Map<TDtoResponse>(updatedEntity);
    }
    #endregion

    #region UpdateList
    public List<TEntity> _UpdateList(IEnumerable<TEntity> entityList)
    {
        return _repository.UpdateAndSave(entityList);
    }

    public List<TDtoResponse> _UpdateList<TDtoResponse>(IEnumerable<TEntity> entityList) where TDtoResponse : IDto
    {
        List<TEntity> updatedList = _repository.UpdateAndSave(entityList);
        return _mapper.Map<List<TDtoResponse>>(updatedList);
    }
    #endregion

    #region Delete
    public void _Delete(TEntity entity)
    {
        _repository.DeleteAndSave(entity);
    }

    public void _Delete(IEnumerable<TEntity> entityList)
    {
        _repository.DeleteAndSave(entityList);
    }

    public void _Delete(Expression<Func<TEntity, bool>> where)
    {
        _repository.DeleteAndSave(where);
    }

    public TEntity _UndoDelete(Expression<Func<TEntity, bool>> where)
    {
        TEntity? originalEntity = _repository.Get(where: where, ignoreFilters: true);

        if (originalEntity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to delete rollback."");
        if (originalEntity is not ISoftDeletableEntity softEntity) throw new GeneralException(""Type of Entity must implement ISoftDeletableEntity for Rollback Delete"");

        softEntity.IsDeleted = false;
        softEntity.DeletedBy = null;
        softEntity.DeletedDateUtc = null;

        return _repository.UpdateAndSave(originalEntity);
    }
    #endregion

    #region IsExist & Count
    public bool _IsExist(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false)
    {
        return _repository.IsExist(filter, where, ignoreFilters);
    }

    public int _Count(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false)
    {
        return _repository.Count(filter, where, ignoreFilters);
    }
    #endregion

    #region Get
    public TEntity? _Get(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true)
    {
        TEntity? entity = _repository.Get(
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return entity;
    }

    public TDtoResponse? _Get<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false) where TDtoResponse : IDto
    {
        TDtoResponse? responseModel = _repository.Get(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return responseModel;
    }

    public object? _Get(
        Expression<Func<TEntity, object>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false)
    {
        object? responseModel = _repository.Get(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return responseModel;
    }

    public TDtoResponse? _Get<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false) where TDtoResponse : IDto
    {
        TDtoResponse? responseModel = _repository.Get<TDtoResponse>(
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return responseModel;
    }
    #endregion

    #region GetList
    public ICollection<TEntity>? _GetList(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true)
    {
        ICollection<TEntity>? entity = _repository.GetAll(
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return entity;
    }

    public ICollection<TDtoResponse>? _GetList<TDtoResponse>(
       Expression<Func<TEntity, TDtoResponse>> select,
       Filter? filter = null,
       IEnumerable<Sort>? sorts = null,
       Expression<Func<TEntity, bool>>? where = null,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
       bool ignoreFilters = false,
       bool tracking = false) where TDtoResponse : IDto
    {
        ICollection<TDtoResponse>? responseModel = _repository.GetAll(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return responseModel;
    }

    public ICollection<object>? _GetList(
       Expression<Func<TEntity, object>> select,
       Filter? filter = null,
       IEnumerable<Sort>? sorts = null,
       Expression<Func<TEntity, bool>>? where = null,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
       bool ignoreFilters = false,
       bool tracking = false)
    {
        ICollection<object>? responseModel = _repository.GetAll(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return responseModel;
    }

    public ICollection<TDtoResponse>? _GetList<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false) where TDtoResponse : IDto
    {
        ICollection<TDtoResponse>? responseModel = _repository.GetAll<TDtoResponse>(
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking);

        return responseModel;
    }
    #endregion

    #region Datatable Server-Side
    public DatatableResponseServerSide<TEntity> _DatatableServerSide(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        return _repository.DatatableServerSide(
            datatableRequest: datatableRequest,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }

    public DatatableResponseServerSide<TDtoResponse> _DatatableServerSide<TDtoResponse>(
       DatatableRequest datatableRequest,
       Expression<Func<TEntity, TDtoResponse>> select,
       Filter? filter = null,
       IEnumerable<Sort>? sorts = null,
       Expression<Func<TEntity, bool>>? where = null,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
       bool ignoreFilters = false) where TDtoResponse : IDto
    {
        return _repository.DatatableServerSide<TDtoResponse>(
            datatableRequest: datatableRequest,
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }

    public DatatableResponseServerSide<TDtoResponse> _DatatableServerSide<TDtoResponse>(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false) where TDtoResponse : IDto
    {
        return _repository.DatatableServerSide<TDtoResponse>(
            datatableRequest: datatableRequest,
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }
    #endregion

    #region Datatable Client-Side
    public DatatableResponseClientSide<TEntity> _DatatableClientSide(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        return _repository.DatatableClientSide(
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }

    public DatatableResponseClientSide<TDtoResponse> _DatatableClientSide<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false) where TDtoResponse : IDto
    {
        return _repository.DatatableClientSide<TDtoResponse>(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }

    public DatatableResponseClientSide<TDtoResponse> _DatatableClientSide<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false) where TDtoResponse : IDto
    {
        return _repository.DatatableClientSide<TDtoResponse>(
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }
    #endregion

    #region Pagination
    public PaginationResponse<TEntity> _Pagination(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false)
    {
        return _repository.Pagination(
            paginationRequest: paginationRequest,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }

    public PaginationResponse<TDtoResponse> _Pagination<TDtoResponse>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false) where TDtoResponse : IDto
    {
        return _repository.Pagination<TDtoResponse>(
            paginationRequest: paginationRequest,
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }

    public PaginationResponse<TDtoResponse> _Pagination<TDtoResponse>(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false) where TDtoResponse : IDto
    {
        return _repository.Pagination<TDtoResponse>(
            paginationRequest: paginationRequest,
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters);
    }
    #endregion

    // ############################# Async Methods #############################
    #region Add
    public async Task<TEntity> _AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return await _repository.AddAndSaveAsync(entity, cancellationToken);
    }

    public async Task<TDtoResponse> _AddAsync<TDtoResponse>(TEntity entity, CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        TEntity insertedEntity = await _repository.AddAndSaveAsync(entity, cancellationToken);
        return _mapper.Map<TDtoResponse>(insertedEntity);
    }

    public async Task<TEntity> _AddAsync<TDtoRequest>(TDtoRequest insertModel, CancellationToken cancellationToken = default) where TDtoRequest : IDto
    {
        TEntity mappedEntity = _mapper.Map<TEntity>(insertModel);
        return await _repository.AddAndSaveAsync(mappedEntity, cancellationToken);
    }

    public async Task<TDtoResponse> _AddAsync<TDtoRequest, TDtoResponse>(TDtoRequest insertModel, CancellationToken cancellationToken = default) where TDtoRequest : IDto where TDtoResponse : IDto
    {
        TEntity mappedEntity = _mapper.Map<TEntity>(insertModel);
        TEntity insertedEntity = await _repository.AddAndSaveAsync(mappedEntity, cancellationToken);
        return _mapper.Map<TDtoResponse>(insertedEntity);
    }
    #endregion

    #region AddList
    public async Task<List<TEntity>> _AddListAsync(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default)
    {
        return await _repository.AddAndSaveAsync(entityList, cancellationToken);
    }

    public async Task<List<TDtoResponse>> _AddListAsync<TDtoResponse>(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        List<TEntity> insertedEntityList = await _repository.AddAndSaveAsync(entityList, cancellationToken);
        return _mapper.Map<List<TDtoResponse>>(insertedEntityList);
    }

    public async Task<List<TEntity>> _AddListAsync<TDtoRequest>(IEnumerable<TDtoRequest> insertModelList, CancellationToken cancellationToken = default) where TDtoRequest : IDto
    {
        IEnumerable<TEntity> mappedEntityList = _mapper.Map<IEnumerable<TEntity>>(insertModelList);
        return await _repository.AddAndSaveAsync(mappedEntityList, cancellationToken);
    }

    public async Task<List<TDtoResponse>> _AddListAsync<TDtoRequest, TDtoResponse>(IEnumerable<TDtoRequest> insertModelList, CancellationToken cancellationToken = default) where TDtoRequest : IDto where TDtoResponse : IDto
    {
        IEnumerable<TEntity> mappedEntityList = _mapper.Map<IEnumerable<TEntity>>(insertModelList);
        List<TEntity> insertedEntityList = await _repository.AddAndSaveAsync(mappedEntityList, cancellationToken);
        return _mapper.Map<List<TDtoResponse>>(insertedEntityList);
    }
    #endregion

    #region Update
    public async Task<TEntity> _UpdateAsync(TEntity entity, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default)
    {
        TEntity? originalEntity = await _repository.GetAsync(where: where, cancellationToken: cancellationToken);
        if (originalEntity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        _mapper.Map(entity, originalEntity);

        return await _repository.UpdateAndSaveAsync(originalEntity, cancellationToken);
    }

    public async Task<TDtoResponse> _UpdateAsync<TDtoResponse>(TEntity entity, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        TEntity? originalEntity = await _repository.GetAsync(where: where, cancellationToken: cancellationToken);
        if (originalEntity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        _mapper.Map(entity, originalEntity);

        TEntity updatedEntity = await _repository.UpdateAndSaveAsync(originalEntity, cancellationToken);
        return _mapper.Map<TDtoResponse>(updatedEntity);
    }

    public async Task<TEntity> _UpdateAsync<TDtoRequest>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default) where TDtoRequest : IDto
    {
        TEntity? entity = await _repository.GetAsync(where: where, cancellationToken: cancellationToken);
        if (entity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        TEntity entityToUpdate = _mapper.Map(updateModel, entity);
        return await _repository.UpdateAndSaveAsync(entityToUpdate, cancellationToken);
    }

    public async Task<TDtoResponse> _UpdateAsync<TDtoRequest, TDtoResponse>(TDtoRequest updateModel, Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default) where TDtoRequest : IDto where TDtoResponse : IDto
    {
        TEntity? entity = await _repository.GetAsync(where: where, cancellationToken: cancellationToken);
        if (entity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to update."");

        TEntity entityToUpdate = _mapper.Map(updateModel, entity);
        TEntity updatedEntity = await _repository.UpdateAndSaveAsync(entityToUpdate, cancellationToken);
        return _mapper.Map<TDtoResponse>(updatedEntity);
    }
    #endregion

    #region UpdateList
    public async Task<List<TEntity>> _UpdateListAsync(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default)
    {
        return await _repository.UpdateAndSaveAsync(entityList, cancellationToken);
    }

    public async Task<List<TDtoResponse>> _UpdateListAsync<TDtoResponse>(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        List<TEntity> updatedList = await _repository.UpdateAndSaveAsync(entityList, cancellationToken);
        return _mapper.Map<List<TDtoResponse>>(updatedList);
    }
    #endregion

    #region Delete
    public async Task _DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAndSaveAsync(entity, cancellationToken);
    }

    public async Task _DeleteAsync(IEnumerable<TEntity> entityList, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAndSaveAsync(entityList, cancellationToken);
    }

    public async Task _DeleteAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAndSaveAsync(where, cancellationToken);
    }

    public async Task<TEntity> _UndoDeleteAsync(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default)
    { 
        TEntity? originalEntity = await _repository.GetAsync(where: where, ignoreFilters: true, cancellationToken: cancellationToken);

        if (originalEntity == null) throw new GeneralException($""The entity({nameof(TEntity)}) was not found to delete rollback."");
        if (originalEntity is not ISoftDeletableEntity softEntity) throw new GeneralException(""Type of Entity must implement ISoftDeletableEntity for Rollback Delete"");

        softEntity.IsDeleted = false;
        softEntity.DeletedBy = null;
        softEntity.DeletedDateUtc = null;

        return await _repository.UpdateAndSaveAsync(originalEntity, cancellationToken);
    }
    #endregion

    #region IsExist & Count
    public async Task<bool> _IsExistAsync(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false, CancellationToken cancellationToken = default)
    {
        return await _repository.IsExistAsync(filter, where, ignoreFilters, cancellationToken);
    }

    public async Task<int> _CountAsync(Filter? filter = null, Expression<Func<TEntity, bool>>? where = null, bool ignoreFilters = false, CancellationToken cancellationToken = default)
    {
        return await _repository.CountAsync(filter, where, ignoreFilters, cancellationToken);
    }
    #endregion

    #region Get
    public async Task<TEntity?> _GetAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default)
    {
        TEntity? entity = await _repository.GetAsync(
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return entity;
    }

    public async Task<TDtoResponse?> _GetAsync<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        TDtoResponse? responseModel = await _repository.GetAsync(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return responseModel;
    }

    public async Task<object?> _GetAsync(
        Expression<Func<TEntity, object>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        object? responseModel = await _repository.GetAsync(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return responseModel;
    }

    public async Task<TDtoResponse?> _GetAsync<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        TDtoResponse? responseModel = await _repository.GetAsync<TDtoResponse>(
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return responseModel;
    }
    #endregion

    #region GetList
    public async Task<ICollection<TEntity>?> _GetListAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = true,
        CancellationToken cancellationToken = default)
    {
        ICollection<TEntity>? entity = await _repository.GetAllAsync(
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return entity;
    }

    public async Task<ICollection<TDtoResponse>?> _GetListAsync<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        ICollection<TDtoResponse>? responseModel = await _repository.GetAllAsync(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return responseModel;
    }

    public async Task<ICollection<object>?> _GetListAsync(
        Expression<Func<TEntity, object>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        ICollection<object>? responseModel = await _repository.GetAllAsync(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return responseModel;
    }

    public async Task<ICollection<TDtoResponse>?> _GetListAsync<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        bool tracking = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        ICollection<TDtoResponse>? responseModel = await _repository.GetAllAsync<TDtoResponse>(
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            tracking: tracking,
            cancellationToken: cancellationToken);

        return responseModel;
    }
    #endregion

    #region Datatable Server-Side
    public async Task<DatatableResponseServerSide<TEntity>> _DatatableServerSideAsync(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default)
    {
        return await _repository.DatatableServerSideAsync(
            datatableRequest: datatableRequest,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }

    public async Task<DatatableResponseServerSide<TDtoResponse>> _DatatableServerSideAsync<TDtoResponse>(
        DatatableRequest datatableRequest,
        Expression<Func<TEntity, TDtoResponse>> select, Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        return await _repository.DatatableServerSideAsync<TDtoResponse>(
            datatableRequest: datatableRequest,
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }

    public async Task<DatatableResponseServerSide<TDtoResponse>> _DatatableServerSideAsync<TDtoResponse>(
        DatatableRequest datatableRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        return await _repository.DatatableServerSideAsync<TDtoResponse>(
            datatableRequest: datatableRequest,
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }
    #endregion

    #region Datatable Client-Side
    public async Task<DatatableResponseClientSide<TEntity>> _DatatableClientSideAsync(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default)
    {
        return await _repository.DatatableClientSideAsync(
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }

    public async Task<DatatableResponseClientSide<TDtoResponse>> _DatatableClientSideAsync<TDtoResponse>(
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        return await _repository.DatatableClientSideAsync<TDtoResponse>(
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }

    public async Task<DatatableResponseClientSide<TDtoResponse>> _DatatableClientSideAsync<TDtoResponse>(
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = true,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        return await _repository.DatatableClientSideAsync<TDtoResponse>(
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }
    #endregion

    #region Pagination
    public async Task<PaginationResponse<TEntity>> _PaginationAsync(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default)
    {
        return await _repository.PaginationAsync(
            paginationRequest: paginationRequest,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }

    public async Task<PaginationResponse<TDtoResponse>> _PaginationAsync<TDtoResponse>(
        PaginationRequest paginationRequest,
        Expression<Func<TEntity, TDtoResponse>> select,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        return await _repository.PaginationAsync<TDtoResponse>(
            paginationRequest: paginationRequest,
            select: select,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }

    public async Task<PaginationResponse<TDtoResponse>> _PaginationAsync<TDtoResponse>(
        PaginationRequest paginationRequest,
        Filter? filter = null,
        IEnumerable<Sort>? sorts = null,
        Expression<Func<TEntity, bool>>? where = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool ignoreFilters = false,
        CancellationToken cancellationToken = default) where TDtoResponse : IDto
    {
        return await _repository.PaginationAsync<TDtoResponse>(
            paginationRequest: paginationRequest,
            configurationProvider: _mapper.ConfigurationProvider,
            filter: filter,
            sorts: sorts,
            where: where,
            orderBy: orderBy,
            include: include,
            ignoreFilters: ignoreFilters,
            cancellationToken: cancellationToken);
    }
    #endregion
}";

        string folderPath = Path.Combine(solutionPath, "Business", "ServiceBase");

        var results = new List<string>
        {
            AddFile(folderPath, "IServiceBase", code_IServiceBase),
            AddFile(folderPath, "IServiceBaseAsync", code_IServiceBaseAsync),
            AddFile(folderPath, "ServiceBase", code_ServiceBase)
        };

        return string.Join("\n", results);
    }

    public string GenerateUtils(string solutionPath)
    {
        var identityTypeConfigs = _appSetting.GetIdentityModelTypeNames(_entityRepository, _fieldRepository);

        if (_appSetting.IsThereIdentiy)
        {
            string code_ITokenService = $@"
using Core.Utils.Auth;
using Model.Entities;
using System.Security.Claims;

namespace Business.Utils.TokenService;

public interface ITokenService
{{
    AccessToken GenerateAccessToken(IList<Claim> claims);
    RefreshToken GenerateRefreshToken({identityTypeConfigs.IdentityUserType} user);
}}";

            string code_TokenService = @"using Core.Utils.Auth;
using Core.Utils.CrossCuttingConcerns;
using Core.Utils.ExceptionHandle.Exceptions;
using Core.Utils.HttpContextManager;
using Microsoft.IdentityModel.Tokens;
using Model.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Business.Utils.TokenService;

[ExceptionHandler]
public class TokenService : ITokenService
{
    private readonly TokenSettings _tokenSettings;
    private readonly HttpContextManager _httpContextManager;
    public TokenService(TokenSettings tokenSettings, HttpContextManager httpContextManager)
    {
        _tokenSettings = tokenSettings;
        _httpContextManager = httpContextManager;
    }


    public AccessToken GenerateAccessToken(IList<Claim> claims)
    {
        DateTime expiration = DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenExpiration);
        SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecurityKey));
        SigningCredentials signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512Signature);

        JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
            issuer: _tokenSettings.Issuer,
            audience: _tokenSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: signingCredentials
        );

        string? token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

        return new AccessToken(token, expiration);
    }

    public RefreshToken GenerateRefreshToken(User user)
    {
        string? ipAddress = _httpContextManager.GetClientIp();
        if (string.IsNullOrEmpty(ipAddress)) throw new GeneralException(""Could not read client ip address for generating refresh token!"");

        return new RefreshToken
        {
            UserId = user.Id,
            IpAddress = ipAddress.Trim(),
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpirationUtc = DateTime.UtcNow.AddMinutes(_tokenSettings.RefreshTokenExpiration),
            CreateDateUtc = DateTime.UtcNow,
            TTL = _tokenSettings.RefreshTokenTTL
        };
    }
}";


            string folderPath = Path.Combine(solutionPath, "Business", "Utils", "TokenService");

            var results = new List<string>
            {
                AddFile(folderPath, "ITokenService", code_ITokenService),
                AddFile(folderPath, "TokenService", code_TokenService)
            };
            return string.Join("\n", results);
        }
        return "";
    }
    #endregion

    public string GenerateMappings(string solutionPath)
    {
        StringBuilder sb = new();

        var dtos = _dtoRepository.GetAll(f => true, include: i => i.Include(x => x.RelatedEntity), enableTracking: false);

        sb.AppendLine("using AutoMapper;");
        sb.AppendLine("using Model.Entities;");
        if (_appSetting.IsThereIdentiy)
            sb.AppendLine("using Model.Auth.SignUp;");
        var groupedByEntity = dtos.GroupBy(f => new { f.RelatedEntity.Name });
        foreach (var gd in groupedByEntity)
        {
            sb.AppendLine($"using Model.Dtos.{gd.Key.Name}_;");
        }
        sb.AppendLine();
        sb.AppendLine("namespace Business.Mappings;");
        sb.AppendLine();
        sb.AppendLine("public class MappingProfiles : Profile");
        sb.AppendLine("{");
        sb.AppendLine("\tpublic MappingProfiles()");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\t// CreateMap<source, dest>");

        var entities = _entityRepository.GetAll(f => true, include: i => i.Include(x => x.Fields).ThenInclude(ti => ti.FieldType), enableTracking: false);
        foreach (var entity in entities)
        {
            sb.AppendLine();
            sb.AppendLine($"\t\t#region {entity.Name}");
            var dtoList = _dtoRepository.GetAll(f => f.RelatedEntityId == entity.Id, enableTracking: false);

            sb.AppendLine($@"CreateMap<{entity.Name}, {entity.Name}>().ForAllMembers(opt => opt.Condition((src, dest, srcMember, destMember) => !Equals(srcMember, destMember)));");
            sb.AppendLine("");

            // Response Dtos Mapping
            // ** Signup to user
            if (_appSetting.IsThereIdentiy && _appSetting.UserEntityId == entity.Id)
            {
                sb.AppendLine($"\t\tCreateMap<SignUpRequest, {entity.Name}>()");
                foreach (var field_user in entity.Fields.Where(f => !f.IsUnique && f.IsRequired && f.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base))
                {
                    sb.AppendLine($"\t\t\t.ForMember(dest => dest.{field_user.Name}, opt => opt.MapFrom(src => src.{field_user.Name}))");
                }
                sb.AppendLine($"\t\t\t.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))");
                sb.AppendLine($"\t\t\t.ReverseMap();");
                sb.AppendLine();
            }

            foreach (var dto in dtoList.Where(f => f.CrudTypeId == (int)CrudTypeEnums.Read))
            {
                GenerateResponseDto(ref sb, entity, dto);
            }

            // Create Update Delete Dtos Mapping
            foreach (var dto in dtoList.Where(f => f.CrudTypeId != (int)CrudTypeEnums.Read))
            {
                GenerateCommandDto(ref sb, entity, dto);
            }
            sb.AppendLine($"\t\t#endregion");
        }
        sb.AppendLine("\t}");
        sb.AppendLine("}");

        string folderPath = Path.Combine(solutionPath, "Business", "Mappings");

        return AddFile(folderPath, "MappingProfiles", sb.ToString());
    }

    public string GeneraterService(string solutionPath)
    {
        var results = new List<string>();

        var roslynBusinessServiceGenerator = new RoslynBusinessServiceGenerator(_appSetting);

        string folderPathAbstract = Path.Combine(solutionPath, "Business", "Abstract");
        string folderPathConcrete = Path.Combine(solutionPath, "Business", "Concrete");

        var entities = _entityRepository.GetAll(f => f.Control == false, include: i => i.Include(x => x.Fields).ThenInclude(y => y.FieldType));

        foreach (var entity in entities)
        {
            var dtos = _dtoRepository.GetAll(
                filter: f => f.RelatedEntityId == entity.Id,
                include: i => i
                    .Include(x => x.DtoFields).ThenInclude(ti => ti.SourceField)
                    .Include(x => x.RelatedEntity).ThenInclude(ti => ti.Fields));

            string code_abstract = roslynBusinessServiceGenerator.GeneraterAbstract(entity, dtos);
            string code_concrete = roslynBusinessServiceGenerator.GeneraterConcrete(entity, dtos);

            results.Add(AddFile(folderPathAbstract, $"I{entity.Name}Service", code_abstract));
            results.Add(AddFile(folderPathConcrete, $"{entity.Name}Service", code_concrete));
        }

        if (_appSetting.IsThereIdentiy)
        {
            string code_IAuthService = @"using Model.Auth.Login;
using Model.Auth.RefreshAuth;
using Model.Auth.SignUp;

namespace Business.Abstract;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default);
    Task<SignUpResponse> SignUpAsync(SignUpRequest signUpRequest, CancellationToken cancellationToken = default);
    Task<RefreshAuthResponse> RefreshAuthAsync(RefreshAuthRequest refreshAuthRequest, CancellationToken cancellationToken = default);
    Task LoginWebBaseAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default);
    Task SignUpWebBaseAsync(SignUpRequest signUpRequest, CancellationToken cancellationToken = default);
}";

            results.Add(AddFile(folderPathAbstract, "IAuthService", code_IAuthService));


            string code_AuthService = roslynBusinessServiceGenerator.GeneraterAuthServiceConcrete();

            results.Add(AddFile(folderPathConcrete, "AuthService", code_AuthService));
        }

        return string.Join("\n", results);
    }

    public string GenerateServiceRegistrations(string solutionPath)
    {
        var results = new List<string>();

        var entities = _entityRepository.GetAll(f => f.Control == false);

        StringBuilder sb = new();
        sb.AppendLine("using Autofac;");
        sb.AppendLine("using Autofac.Extras.DynamicProxy;");
        sb.AppendLine("using Business.Abstract;");
        sb.AppendLine("using Business.Concrete;");
        if (_appSetting.IsThereIdentiy) sb.AppendLine("using Business.Utils.TokenService;");
        sb.AppendLine("using Core.Utils.CrossCuttingConcerns;");
        sb.AppendLine();
        sb.AppendLine("namespace Business;");
        sb.AppendLine();
        sb.AppendLine("public class AutofacModule : Module");
        sb.AppendLine("{");
        sb.AppendLine("\tprotected override void Load(ContainerBuilder builder)");
        sb.AppendLine("\t{");
        if (_appSetting.IsThereIdentiy)
        {
            sb.AppendLine($"\t\tbuilder.RegisterType<TokenService>().As<ITokenService>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(ExceptionHandlerInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
            sb.AppendLine();

            sb.AppendLine($"\t\tbuilder.RegisterType<AuthService>().As<IAuthService>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(ValidationInterceptor), typeof(ExceptionHandlerInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
            sb.AppendLine();
        }
        sb.AppendLine("\t\t// ***** Entity Services *****");
        foreach (var entity in entities)
        {
            sb.AppendLine($"\t\tbuilder.RegisterType<{entity.Name}Service>().As<I{entity.Name}Service>()");
            sb.AppendLine("\t\t\t.EnableInterfaceInterceptors()");
            sb.AppendLine("\t\t\t.InterceptedBy(typeof(ValidationInterceptor), typeof(ExceptionHandlerInterceptor), typeof(CacheRemoveInterceptor), typeof(CacheRemoveGroupInterceptor), typeof(CacheInterceptor))");
            sb.AppendLine("\t\t\t.InstancePerLifetimeScope();");
            sb.AppendLine();
        }
        sb.AppendLine("\t}");
        sb.AppendLine("}");

        string code_ServiceRegistration = @"using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Business;

public static class ServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}";


        string folderPath = Path.Combine(solutionPath, "Business");
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
                return $"OK: File {fileName} added to Business project.";
            }
            else
            {
                return $"INFO: File {fileName} already exists in Business project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while adding file({fileName}) to Business project. \n Details:{ex.Message}");
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
                return $"OK: File {fileName} removed from Business project.";
            }
            else
            {
                return $"INFO: File {fileName} does not exist in Business project.";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"ERROR: An error occurred while removing file ({fileName}) from Business project. \n Details: {ex.Message}");
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


    private void GenerateResponseDto(ref StringBuilder sb, Entity entity, Dto dto)
    {
        var dtoFields = _dtoFieldRepository.GetAll(
            filter: f => f.DtoId == dto.Id,
            include: i => i.Include(x => x.SourceField).ThenInclude(x => x.Entity).Include(x => x.SourceField).ThenInclude(x => x.FieldType),
            enableTracking: false);

        sb.AppendLine($"\t\tCreateMap<{entity.Name}, {dto.Name}>()");

        foreach (var dtoField in dtoFields)
        {
            // NOT: DtoField.SourceField ya baze(int,string) ya da dto tipinde olabilir entity tipi tanımlı olmasına rağmen bulundurmayacak

            // 1) DtoField kenidi entitisi içinde blunuyorsa doğrudan src referansı ile kullanabiliriz
            if (entity.Id == dtoField.SourceField.EntityId)
            {
                // a) Source field base tipinde ise (string, vb.), 
                if (dtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)
                {
                    sb.AppendLine($"\t\t\t.ForMember(dest => dest.{dtoField.Name}, opt => opt.MapFrom(src => src.{dtoField.SourceField.Name}))");
                }
                // b) Eğer ilgili entitinin bir dto'su ise
                else if (dtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Dto)
                {
                    sb.AppendLine($"\t\t\t.ForMember(dest => dest.{dtoField.Name}, opt => opt.MapFrom(src => src))");
                }
            }
            // 2) DtoField farklı bir entitiden den sağlanıyor
            else
            {
                // Not: Source field base veya dto olması fark etmiyor

                var dtoFieldRelations = _dtoFieldRepository.GetDtoFieldRelations(dtoField.Id);
                if (dtoFieldRelations == null || !dtoFieldRelations.Any()) throw new Exception($"Cannot find relations between entities ({entity.Name}, {dtoField.SourceField.Entity.Name})");

                sb.AppendLine($"\t\t\t.ForMember(");
                sb.AppendLine($"\t\t\t\tdest => dest.{dtoField.Name},");

                // a.1) ilk ilişki koşulu
                var dfrFirst = dtoFieldRelations.First();

                bool isLastRel = dtoFieldRelations.Count == 1;
                bool controlOfRelationFirst = dfrFirst.Relation.PrimaryField.EntityId == entity.Id;

                string destPropOfFirst = controlOfRelationFirst ? dfrFirst.Relation.PrimaryEntityVirPropName : dfrFirst.Relation.ForeignEntityVirPropName;

                sb.AppendLine($"\t\t\t\topt => opt.MapFrom(src => src.{destPropOfFirst} != default ?");

                if (isLastRel && dfrFirst.DtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)
                    sb.AppendLine($"\t\t\t\t\tsrc.{destPropOfFirst}.{dfrFirst.DtoField.SourceField.Name}");
                else
                    sb.AppendLine($"\t\t\t\t\tsrc.{destPropOfFirst}");


                // a.2) diğer ilişkiler
                bool isFoundList = dfrFirst.Relation.RelationTypeId == (int)RelationTypeEnums.OneToMany;

                int lastDestEntityId = controlOfRelationFirst ? dfrFirst.Relation.ForeignField.EntityId : dfrFirst.Relation.PrimaryField.EntityId;

                for (int i = 1; i < dtoFieldRelations.Count; i++)
                {
                    var dfr = dtoFieldRelations[i];

                    isLastRel = i + 1 == dtoFieldRelations.Count;

                    bool controlOfRelation = dfr.Relation.PrimaryField.EntityId == lastDestEntityId;

                    string srcProp = controlOfRelation ? dfr.Relation.ForeignEntityVirPropName : dfr.Relation.PrimaryEntityVirPropName;
                    string destProp = controlOfRelation ? dfr.Relation.PrimaryEntityVirPropName : dfr.Relation.ForeignEntityVirPropName;

                    bool isDestList =
                        dfr.Relation.RelationTypeId == (int)RelationTypeEnums.OneToMany &&
                        controlOfRelation;
                    // primarykeyin olduğu entity destination ise list olmalalı
                    string tabs = string.Concat(Enumerable.Repeat("\t", i + 5));
                    if (isFoundList == false)
                    {
                        if (isLastRel && dfr.DtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)
                            sb.AppendLine($"{tabs}{destProp}.{dfr.DtoField.SourceField.Name}");
                        else
                            sb.AppendLine($"{tabs}.{destProp}");
                    }
                    else
                    {
                        if (isDestList)
                        {
                            if (isLastRel && dfr.DtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)
                                sb.AppendLine($"{tabs}.SelectMany(x => x.{destProp}.{dfr.DtoField.SourceField.Name})");
                            else
                                sb.AppendLine($"{tabs}.SelectMany(x => x.{destProp})");
                        }
                        else
                        {
                            if (isLastRel && dfr.DtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)
                                sb.AppendLine($"{tabs}.Select(x => x.{destProp}.{dfr.DtoField.SourceField.Name})");
                            else
                                sb.AppendLine($"{tabs}.Select(x => x.{destProp})");
                        }
                    }

                    lastDestEntityId = controlOfRelation ? dfr.Relation.ForeignField.EntityId : dfr.Relation.PrimaryField.EntityId;

                    if (isFoundList == false)
                    {
                        isFoundList = dfr.Relation.RelationTypeId == (int)RelationTypeEnums.OneToMany;
                    }
                }

                sb.AppendLine($"\t\t\t\t\t: default");
                sb.AppendLine($"\t\t\t\t)");
                sb.AppendLine($"\t\t\t)");
            }
        }
        sb.AppendLine("\t\t\t.ForAllMembers(opt => opt.Condition((src, dest, srcMember, destMember) => !Equals(srcMember, destMember)));");
        sb.AppendLine();
    }

    private void GenerateCommandDto(ref StringBuilder sb, Entity entity, Dto dto)
    {
        var dtoFields = _dtoFieldRepository.GetAll(
                            filter: f => f.DtoId == dto.Id,
                            include: i => i.Include(x => x.SourceField).ThenInclude(x => x.FieldType),
                            enableTracking: false);

        sb.AppendLine($"\t\tCreateMap<{dto.Name}, {entity.Name}>()");
        foreach (var dtoField in dtoFields)
        {
            // a) Source field base tipinde ise (string, vb.), 
            if (dtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)
            {
                sb.AppendLine($"\t\t\t.ForMember(dest => dest.{dtoField.Name}, opt => opt.MapFrom(src => src.{dtoField.SourceField.Name}))");
            }
            // b) Eğer ilgili entitinin bir dto'su ise
            else if (dtoField.SourceField.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Dto)
            {
                sb.AppendLine($"\t\t\t.ForMember(dest => dest.{dtoField.Name}, opt => opt.MapFrom(src => src))");
            }
        }
        sb.AppendLine("\t\t\t.ReverseMap();");
        sb.AppendLine();
    }
}