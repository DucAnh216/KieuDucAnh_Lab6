using ASC.Model.Models;
using ASC.Web.Areas.Configuration.Models;
using Microsoft.AspNetCore.Mvc;
using ASC.Business.Interfaces;
using ASC.Web.Controllers;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;

namespace ASC.Web.Areas.Configuration.Controllers
{
    [Area("Configuration")]
    [Authorize(Roles = "Admin")]
    public class MasterDataController : BaseController
    {
        private readonly IMasterDataOperations _masterData;
        private readonly IMapper _mapper;
        private readonly ILogger<MasterDataController> _logger;

        public MasterDataController(
            IMasterDataOperations masterData,
            IMapper mapper,
            ILogger<MasterDataController> logger)
        {
            _masterData = masterData ?? throw new ArgumentNullException(nameof(masterData));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ----------------------- MASTER KEY -----------------------

        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            try
            {
                var masterKeys = await _masterData.GetAllMasterKeysAsync() ?? new List<MasterDataKey>();
                var viewModel = new MasterKeysViewModel
                {
                    MasterKeys = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys),
                    IsEdit = false,
                    MasterKeyInContext = new MasterDataKeyViewModel()
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy Master Keys.");
                return View(new MasterKeysViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel model)
        {
            try
            {
                if (model.MasterKeyInContext == null || string.IsNullOrWhiteSpace(model.MasterKeyInContext.Name))
                {
                    ModelState.AddModelError("", "Tên Master Key không được để trống.");
                    var allKeys = await _masterData.GetAllMasterKeysAsync() ?? new List<MasterDataKey>();
                    model.MasterKeys = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(allKeys);
                    return View(model);
                }

                var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

                if (!model.IsEdit)
                {
                    var newKey = new MasterDataKey
                    {
                        PartitionKey = Guid.NewGuid().ToString(),
                        RowKey = Guid.NewGuid().ToString(),
                        Name = model.MasterKeyInContext.Name,
                        IsActive = model.MasterKeyInContext.IsActive,
                        CreatedBy = user,
                        UpdatedBy = user,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };

                    await _masterData.InsertMasterKeyAsync(newKey);
                }
                else
                {
                    var partitionKey = model.MasterKeyInContext.PartitionKey ?? string.Empty;
                    var rowKey = model.MasterKeyInContext.RowKey ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
                    {
                        ModelState.AddModelError("", "PartitionKey và RowKey không được để trống khi cập nhật.");
                        var allKeys = await _masterData.GetAllMasterKeysAsync() ?? new List<MasterDataKey>();
                        model.MasterKeys = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(allKeys);
                        return View(model);
                    }

                    var existing = await _masterData.GetMasterKeyByIdAsync(partitionKey, rowKey);
                    if (existing != null)
                    {
                        existing.Name = model.MasterKeyInContext.Name;
                        existing.IsActive = model.MasterKeyInContext.IsActive;
                        existing.UpdatedBy = user;
                        existing.UpdatedDate = DateTime.UtcNow;

                        await _masterData.UpdateMasterKeyAsync(existing.PartitionKey, existing.RowKey, existing);
                    }
                }

                return RedirectToAction(nameof(MasterKeys));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý MasterKeys");
                var keys = await _masterData.GetAllMasterKeysAsync() ?? new List<MasterDataKey>();
                model.MasterKeys = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(keys);
                return View(model);
            }
        }

        // ----------------------- MASTER VALUE -----------------------

        [HttpGet]
        public async Task<IActionResult> MasterValues(string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                ModelState.AddModelError("", "PartitionKey không hợp lệ.");
                return View(new MasterValuesViewModel());
            }

            try
            {
                var values = await _masterData.GetMasterValuesByKeyAsync(partitionKey) ?? new List<MasterDataValue>();
                var model = new MasterValuesViewModel
                {
                    PartitionKey = partitionKey,
                    MasterValues = _mapper.Map<List<MasterDataValue>, List<MasterDataValueViewModel>>(values)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách Master Values.");
                return View(new MasterValuesViewModel { PartitionKey = partitionKey });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(MasterValuesViewModel model)
        {
            _logger.LogInformation("POST MasterValues: Name={Name}, Value={Value}, IsActive={IsActive}, PartitionKey={PartitionKey}, RowKey={RowKey}",
                model.Name, model.Value, model.IsActive, model.PartitionKey, model.RowKey);

            if (string.IsNullOrWhiteSpace(model.PartitionKey))
            {
                ModelState.AddModelError("", "PartitionKey không được để trống.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Value))
            {
                ModelState.AddModelError("", "Tên và Giá trị không được để trống.");
                model.MasterValues = _mapper.Map<List<MasterDataValue>, List<MasterDataValueViewModel>>(
                    await _masterData.GetMasterValuesByKeyAsync(model.PartitionKey) ?? new List<MasterDataValue>());
                return View(model);
            }

            var user = HttpContext.User?.Identity?.Name ?? "Anonymous";
            var now = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrEmpty(model.RowKey)) // Thêm mới
                {
                    var entity = new MasterDataValue
                    {
                        PartitionKey = model.PartitionKey,
                        RowKey = Guid.NewGuid().ToString(),
                        Name = model.Name,
                        Value = model.Value,
                        IsActive = model.IsActive,
                        CreatedBy = user,
                        UpdatedBy = user,
                        CreatedDate = now,
                        UpdatedDate = now
                    };

                    await _masterData.InsertMasterValueAsync(entity);
                    _logger.LogInformation("Đã thêm Master Value thành công.");
                }
                else // Cập nhật
                {
                    var list = await _masterData.GetMasterValuesByKeyAsync(model.PartitionKey);
                    var existing = list?.FirstOrDefault(x => x.RowKey == model.RowKey);

                    if (existing == null)
                    {
                        ModelState.AddModelError("", "Không tìm thấy bản ghi cần cập nhật.");
                        model.MasterValues = _mapper.Map<List<MasterDataValue>, List<MasterDataValueViewModel>>(list ?? new List<MasterDataValue>());
                        return View(model);
                    }

                    existing.Name = model.Name;
                    existing.Value = model.Value;
                    existing.IsActive = model.IsActive;
                    existing.UpdatedBy = user;
                    existing.UpdatedDate = now;

                    await _masterData.UpdateMasterValueAsync(existing.PartitionKey, existing.RowKey, existing);
                    _logger.LogInformation("Đã cập nhật Master Value.");
                }

                return RedirectToAction(nameof(MasterValues), new { partitionKey = model.PartitionKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu Master Value.");
                model.MasterValues = _mapper.Map<List<MasterDataValue>, List<MasterDataValueViewModel>>(
                    await _masterData.GetMasterValuesByKeyAsync(model.PartitionKey) ?? new List<MasterDataValue>());
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                ModelState.AddModelError("", "PartitionKey không hợp lệ.");
                return RedirectToAction(nameof(MasterValues), new { partitionKey });
            }

            if (excelFile == null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn file Excel.");
                return RedirectToAction(nameof(MasterValues), new { partitionKey });
            }

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet == null || worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
                {
                    ModelState.AddModelError("", "File Excel không hợp lệ.");
                    return RedirectToAction(nameof(MasterValues), new { partitionKey });
                }

                var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    var name = worksheet.Cells[row, 1].Text?.Trim();
                    var value = worksheet.Cells[row, 2].Text?.Trim();
                    var isActiveStr = worksheet.Cells[row, 3].Text?.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value)) continue;

                    var isActive = string.Equals(isActiveStr, "true", StringComparison.OrdinalIgnoreCase) || isActiveStr == "1";

                    var entity = new MasterDataValue
                    {
                        PartitionKey = partitionKey,
                        RowKey = Guid.NewGuid().ToString(),
                        Name = name,
                        Value = value,
                        IsActive = isActive,
                        CreatedBy = user,
                        UpdatedBy = user,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };

                    await _masterData.InsertMasterValueAsync(entity);
                    _logger.LogInformation("Import: {Name} - {Value} - {IsActive}", name, value, isActive);
                }

                return RedirectToAction(nameof(MasterValues), new { partitionKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import Excel.");
                return RedirectToAction(nameof(MasterValues), new { partitionKey });
            }
        }
    }
}
