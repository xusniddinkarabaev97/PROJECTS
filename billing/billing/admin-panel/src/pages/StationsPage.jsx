import React, { useState, useEffect, useCallback } from "react";
import { useTranslation } from "../i18n/LanguageContext";
import { request } from "../api/client";

const FUEL_TYPE_KEY_MAP = {
  "AI-80": "ai80",
  "AI-92": "ai92",
  "AI-95": "ai95",
  Diesel: "diesel",
  Gas: "gas",
};

const FUEL_TYPES = [
  { value: "AI-80", key: "ai80" },
  { value: "AI-92", key: "ai92" },
  { value: "AI-95", key: "ai95" },
  { value: "Diesel", key: "diesel" },
  { value: "Gas", key: "gas" },
];

const EMPTY_STATION_FORM = {
  name: "",
  address: "",
  region: "",
  phone: "",
  latitude: "",
  longitude: "",
};

const EMPTY_COLUMN_FORM = {
  name: "",
  fuelType: "",
  pricePerLiter: "",
  columnNumber: "",
};

export default function StationsPage() {
  const { t } = useTranslation();

  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [columnsCache, setColumnsCache] = useState({});
  const [expandedStationId, setExpandedStationId] = useState(null);

  const [stationModalOpen, setStationModalOpen] = useState(false);
  const [editingStation, setEditingStation] = useState(null);
  const [stationForm, setStationForm] = useState(EMPTY_STATION_FORM);
  const [stationFormError, setStationFormError] = useState(null);
  const [stationSaving, setStationSaving] = useState(false);

  const [columnModalOpen, setColumnModalOpen] = useState(false);
  const [editingColumn, setEditingColumn] = useState(null);
  const [columnFormStationId, setColumnFormStationId] = useState(null);
  const [columnForm, setColumnForm] = useState(EMPTY_COLUMN_FORM);
  const [columnFormError, setColumnFormError] = useState(null);
  const [columnSaving, setColumnSaving] = useState(false);

  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  const [toast, setToast] = useState(null);

  const showToast = useCallback((message, type) => {
    setToast({ message, type: type || "success" });
    setTimeout(() => setToast(null), 3500);
  }, []);

  const fetchStations = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await request("/v1/stations");
      const list = Array.isArray(data)
        ? data
        : (data?.data ?? data?.items ?? []);
      setStations(list);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStations();
  }, [fetchStations]);

  const fetchColumns = useCallback(async (stationId) => {
    setColumnsCache((prev) => ({
      ...prev,
      [stationId]: { columns: [], loading: true, error: null },
    }));
    try {
      const data = await request(`/v1/stations/${stationId}/columns`);
      const list = Array.isArray(data)
        ? data
        : (data?.data ?? data?.items ?? []);
      setColumnsCache((prev) => ({
        ...prev,
        [stationId]: { columns: list, loading: false, error: null },
      }));
    } catch (err) {
      setColumnsCache((prev) => ({
        ...prev,
        [stationId]: { columns: [], loading: false, error: err.message },
      }));
    }
  }, []);

  const toggleExpand = useCallback(
    (stationId) => {
      setExpandedStationId((prev) => {
        const next = prev === stationId ? null : stationId;
        if (next && !columnsCache[next]) {
          fetchColumns(next);
        }
        return next;
      });
    },
    [columnsCache, fetchColumns],
  );

  const openStationModal = useCallback((station) => {
    if (station) {
      setEditingStation(station);
      setStationForm({
        name: station.name ?? "",
        address: station.address ?? "",
        region: station.region ?? "",
        phone: station.phone ?? "",
        latitude: station.latitude != null ? String(station.latitude) : "",
        longitude: station.longitude != null ? String(station.longitude) : "",
      });
    } else {
      setEditingStation(null);
      setStationForm(EMPTY_STATION_FORM);
    }
    setStationFormError(null);
    setStationModalOpen(true);
  }, []);

  const closeStationModal = useCallback(() => {
    setStationModalOpen(false);
    setEditingStation(null);
    setStationForm(EMPTY_STATION_FORM);
    setStationFormError(null);
  }, []);

  const handleStationFormChange = useCallback((field, value) => {
    setStationForm((prev) => ({ ...prev, [field]: value }));
  }, []);

  const handleStationSubmit = useCallback(
    async (e) => {
      e.preventDefault();
      setStationFormError(null);

      const { name, address, region, phone, latitude, longitude } = stationForm;
      if (!name.trim()) {
        setStationFormError(t("stations.name") + " talab qilinadi");
        return;
      }

      const body = {
        name: name.trim(),
        address: address.trim() || undefined,
        region: region.trim() || undefined,
        phone: phone.trim() || undefined,
        latitude:
          latitude !== "" && latitude != null
            ? parseFloat(latitude)
            : undefined,
        longitude:
          longitude !== "" && longitude != null
            ? parseFloat(longitude)
            : undefined,
      };

      try {
        setStationSaving(true);
        if (editingStation) {
          await request(`/v1/stations/${editingStation.id}`, {
            method: "PUT",
            body,
          });
          showToast(t("stations.stationSaved"));
        } else {
          await request("/v1/stations", {
            method: "POST",
            body,
          });
          showToast(t("stations.stationSaved"));
        }
        closeStationModal();
        fetchStations();
      } catch (err) {
        setStationFormError(err.message);
      } finally {
        setStationSaving(false);
      }
    },
    [
      stationForm,
      editingStation,
      t,
      showToast,
      closeStationModal,
      fetchStations,
    ],
  );

  const openColumnModal = useCallback((stationId, column) => {
    setColumnFormStationId(stationId);
    if (column) {
      setEditingColumn(column);
      setColumnForm({
        name: column.name ?? "",
        fuelType: column.fuelType ?? "",
        pricePerLiter:
          column.pricePerLiter != null ? String(column.pricePerLiter) : "",
        columnNumber:
          column.columnNumber != null ? String(column.columnNumber) : "",
      });
    } else {
      setEditingColumn(null);
      setColumnForm(EMPTY_COLUMN_FORM);
    }
    setColumnFormError(null);
    setColumnModalOpen(true);
  }, []);

  const closeColumnModal = useCallback(() => {
    setColumnModalOpen(false);
    setEditingColumn(null);
    setColumnFormStationId(null);
    setColumnForm(EMPTY_COLUMN_FORM);
    setColumnFormError(null);
  }, []);

  const handleColumnFormChange = useCallback((field, value) => {
    setColumnForm((prev) => ({ ...prev, [field]: value }));
  }, []);

  const handleColumnSubmit = useCallback(
    async (e) => {
      e.preventDefault();
      setColumnFormError(null);

      const { name, fuelType, pricePerLiter, columnNumber } = columnForm;
      if (!name.trim()) {
        setColumnFormError(t("stations.columnName") + " talab qilinadi");
        return;
      }
      if (!fuelType) {
        setColumnFormError(t("stations.selectFuelType"));
        return;
      }

      const body = {
        name: name.trim(),
        fuelType,
        pricePerLiter:
          pricePerLiter !== "" && pricePerLiter != null
            ? parseFloat(pricePerLiter)
            : undefined,
        columnNumber:
          columnNumber !== "" && columnNumber != null
            ? columnNumber.trim()
            : undefined,
      };

      try {
        setColumnSaving(true);
        if (editingColumn) {
          await request(
            `/v1/stations/${columnFormStationId}/columns/${editingColumn.id}`,
            { method: "PUT", body },
          );
          showToast(t("stations.columnSaved"));
        } else {
          await request(`/v1/stations/${columnFormStationId}/columns`, {
            method: "POST",
            body,
          });
          showToast(t("stations.columnSaved"));
        }
        closeColumnModal();
        if (columnFormStationId) {
          fetchColumns(columnFormStationId);
        }
        fetchStations();
      } catch (err) {
        setColumnFormError(err.message);
      } finally {
        setColumnSaving(false);
      }
    },
    [
      columnForm,
      editingColumn,
      columnFormStationId,
      t,
      showToast,
      closeColumnModal,
      fetchColumns,
      fetchStations,
    ],
  );

  const openDeleteModal = useCallback((target) => {
    setDeleteTarget(target);
    setDeleteModalOpen(true);
  }, []);

  const closeDeleteModal = useCallback(() => {
    setDeleteModalOpen(false);
    setDeleteTarget(null);
  }, []);

  const handleDeleteConfirm = useCallback(async () => {
    if (!deleteTarget) return;
    setDeleteLoading(true);
    try {
      if (deleteTarget.type === "station") {
        await request(`/v1/stations/${deleteTarget.id}`, {
          method: "DELETE",
        });
        showToast(t("stations.stationDeleted"));
        setExpandedStationId(null);
        fetchStations();
      } else if (deleteTarget.type === "column") {
        await request(
          `/v1/stations/${deleteTarget.stationId}/columns/${deleteTarget.id}`,
          { method: "DELETE" },
        );
        showToast(t("stations.columnDeleted"));
        fetchColumns(deleteTarget.stationId);
        fetchStations();
      }
      closeDeleteModal();
    } catch (err) {
      showToast(err.message, "error");
    } finally {
      setDeleteLoading(false);
    }
  }, [
    deleteTarget,
    t,
    showToast,
    closeDeleteModal,
    fetchStations,
    fetchColumns,
  ]);

  const getStatusLabel = (station) => {
    if (typeof station.active === "boolean") {
      return station.active ? t("stations.active") : t("stations.inactive");
    }
    const st = station.status;
    if (st === "active" || st === true) return t("stations.active");
    if (st === "inactive" || st === false) return t("stations.inactive");
    return st ?? t("stations.active");
  };

  const getStatusBadgeStyle = (station) => {
    const isActive =
      typeof station.active === "boolean"
        ? station.active
        : station.status === "active" ||
          station.status === true ||
          station.status == null;
    return isActive ? "badge-success" : "badge-muted";
  };

  return (
    <div style={s.container}>
      {toast && (
        <div style={s.toastContainer}>
          <div
            style={{
              ...s.toast,
              ...(toast.type === "error" ? s.toastError : s.toastSuccess),
            }}
          >
            {toast.message}
          </div>
        </div>
      )}

      <div className="page-header">
        <button
          className="btn btn-primary"
          onClick={() => openStationModal(null)}
        >
          + {t("stations.addStation")}
        </button>
      </div>

      {loading ? (
        <div className="loading-container">
          <div className="spinner spinner-lg" />
          <span>{t("common.loading")}</span>
        </div>
      ) : error ? (
        <div className="empty-state">
          <div className="empty-state-icon">{String.fromCodePoint(0x26a0)}</div>
          <div className="empty-state-text text-danger">
            {t("common.error")}
          </div>
          <div className="text-muted text-sm mt-2">{error}</div>
          <button className="btn btn-outline mt-4" onClick={fetchStations}>
            {t("common.refresh")}
          </button>
        </div>
      ) : stations.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">{String.fromCodePoint(0x26fd)}</div>
          <div className="empty-state-text">{t("common.noData")}</div>
        </div>
      ) : (
        <div className="table-container">
          <table>
            <thead>
              <tr>
                <th>{t("stations.name")}</th>
                <th>{t("stations.address")}</th>
                <th>{t("stations.region")}</th>
                <th>{t("stations.phone")}</th>
                <th>{t("stations.columnsCount")}</th>
                <th>{t("common.status")}</th>
                <th>{t("common.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {stations.map((station) => {
                const isExpanded = expandedStationId === station.id;
                const colCache = columnsCache[station.id];
                return (
                  <React.Fragment key={station.id}>
                    <tr
                      onClick={() => toggleExpand(station.id)}
                      style={{
                        cursor: "pointer",
                        background: isExpanded ? "#1c2333" : "transparent",
                      }}
                    >
                      <td style={{ fontWeight: 500 }}>{station.name || "-"}</td>
                      <td className="text-muted">{station.address || "-"}</td>
                      <td>{station.region || "-"}</td>
                      <td>{station.phone || "-"}</td>
                      <td>
                        <span
                          style={{
                            ...s.columnsBadge,
                            ...(isExpanded ? s.columnsBadgeExpanded : {}),
                          }}
                        >
                          {station.columnsCount ??
                            station.columnCount ??
                            station._count?.columns ??
                            (colCache ? colCache.columns.length : "-")}
                        </span>
                      </td>
                      <td>
                        <span
                          className={`badge ${getStatusBadgeStyle(station)}`}
                        >
                          {getStatusLabel(station)}
                        </span>
                      </td>
                      <td>
                        <div
                          className="flex gap-1"
                          onClick={(e) => e.stopPropagation()}
                        >
                          <button
                            className="btn btn-outline btn-sm"
                            onClick={() => openStationModal(station)}
                            title={t("common.edit")}
                          >
                            {String.fromCodePoint(0x270f)}
                          </button>
                          <button
                            className="btn btn-danger btn-sm"
                            onClick={() =>
                              openDeleteModal({
                                type: "station",
                                id: station.id,
                                name: station.name,
                              })
                            }
                            title={t("common.delete")}
                          >
                            {String.fromCodePoint(0x1f5d1)}
                          </button>
                        </div>
                      </td>
                    </tr>

                    {isExpanded && (
                      <tr>
                        <td colSpan={7} style={s.expandedCell}>
                          <div style={s.columnsSection}>
                            <div className="flex justify-between items-center mb-3">
                              <h3 style={s.columnsTitle}>
                                {String.fromCodePoint(0x26fd)}{" "}
                                {t("stations.columns")} — {station.name}
                              </h3>
                              <button
                                className="btn btn-primary btn-sm"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  openColumnModal(station.id, null);
                                }}
                              >
                                + {t("stations.addColumn")}
                              </button>
                            </div>

                            {!colCache || colCache.loading ? (
                              <div
                                style={{
                                  display: "flex",
                                  alignItems: "center",
                                  justifyContent: "center",
                                  padding: "24px",
                                  gap: "8px",
                                  color: "#8b949e",
                                }}
                              >
                                <div className="spinner" />
                                <span>{t("common.loading")}</span>
                              </div>
                            ) : colCache.error ? (
                              <div
                                style={{
                                  textAlign: "center",
                                  padding: "24px",
                                  color: "#f85149",
                                }}
                              >
                                {colCache.error}
                                <button
                                  className="btn btn-outline btn-sm mt-2"
                                  style={{ marginLeft: "8px" }}
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    fetchColumns(station.id);
                                  }}
                                >
                                  {t("common.refresh")}
                                </button>
                              </div>
                            ) : colCache.columns.length === 0 ? (
                              <div
                                style={{
                                  textAlign: "center",
                                  padding: "24px",
                                  color: "#6e7681",
                                }}
                              >
                                {t("common.noData")}
                              </div>
                            ) : (
                              <table style={s.columnsTable}>
                                <thead>
                                  <tr>
                                    <th>{t("stations.columnName")}</th>
                                    <th>{t("stations.fuelType")}</th>
                                    <th>{t("stations.pricePerLiter")}</th>
                                    <th>{t("stations.columnNumber")}</th>
                                    <th>QR</th>
                                    <th>{t("common.status")}</th>
                                    <th>{t("common.actions")}</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  {colCache.columns.map((col) => (
                                    <tr key={col.id}>
                                      <td>{col.name || "-"}</td>
                                      <td>
                                        <span style={s.fuelBadge}>
                                          {t(
                                            "stations.fuelTypes." +
                                              (FUEL_TYPE_KEY_MAP[
                                                col.fuelType
                                              ] || col.fuelType),
                                          ) || col.fuelType}
                                        </span>
                                      </td>
                                      <td className="font-mono">
                                        {col.pricePerLiter != null
                                          ? Number(
                                              col.pricePerLiter,
                                            ).toLocaleString()
                                          : "-"}
                                      </td>
                                      <td className="font-mono">
                                        {col.columnNumber ?? "-"}
                                      </td>
                                      <td style={{ textAlign: "center" }}>
                                        <a
                                          href={`/pay/${col.id}`}
                                          target="_blank"
                                          rel="noopener noreferrer"
                                          title="QR code — open payment page"
                                        >
                                          <img
                                            src={`/api/qr/${col.id}`}
                                            alt="QR"
                                            style={{
                                              width: 64,
                                              height: 64,
                                              cursor: "pointer",
                                              borderRadius: 4,
                                            }}
                                          />
                                        </a>
                                      </td>
                                      <td>
                                        <span
                                          className={`badge ${
                                            col.active !== false
                                              ? "badge-success"
                                              : "badge-muted"
                                          }`}
                                        >
                                          {col.active !== false
                                            ? t("stations.active")
                                            : t("stations.inactive")}
                                        </span>
                                      </td>
                                      <td>
                                        <div className="flex gap-1">
                                          <button
                                            className="btn btn-outline btn-sm"
                                            onClick={(e) => {
                                              e.stopPropagation();
                                              openColumnModal(station.id, col);
                                            }}
                                            title={t("common.edit")}
                                          >
                                            {String.fromCodePoint(0x270f)}
                                          </button>
                                          <button
                                            className="btn btn-danger btn-sm"
                                            onClick={(e) => {
                                              e.stopPropagation();
                                              openDeleteModal({
                                                type: "column",
                                                id: col.id,
                                                stationId: station.id,
                                                name: col.name,
                                              });
                                            }}
                                            title={t("common.delete")}
                                          >
                                            {String.fromCodePoint(0x1f5d1)}
                                          </button>
                                        </div>
                                      </td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            )}
                          </div>
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {stationModalOpen && (
        <div style={s.overlay} onClick={closeStationModal}>
          <div style={s.modal} onClick={(e) => e.stopPropagation()}>
            <div style={s.modalHeader}>
              <h2 style={s.modalTitle}>
                {editingStation
                  ? t("stations.editStation")
                  : t("stations.addStation")}
              </h2>
              <button style={s.modalCloseBtn} onClick={closeStationModal}>
                &times;
              </button>
            </div>
            <form onSubmit={handleStationSubmit}>
              {stationFormError && (
                <div style={s.formError}>{stationFormError}</div>
              )}
              <div style={s.formBody}>
                <div className="form-group">
                  <label>{t("stations.name")} *</label>
                  <input
                    type="text"
                    value={stationForm.name}
                    onChange={(e) =>
                      handleStationFormChange("name", e.target.value)
                    }
                    placeholder={t("stations.name")}
                  />
                </div>

                <div className="form-group">
                  <label>{t("stations.address")}</label>
                  <input
                    type="text"
                    value={stationForm.address}
                    onChange={(e) =>
                      handleStationFormChange("address", e.target.value)
                    }
                    placeholder={t("stations.address")}
                  />
                </div>

                <div className="form-group">
                  <label>{t("stations.region")}</label>
                  <input
                    type="text"
                    value={stationForm.region}
                    onChange={(e) =>
                      handleStationFormChange("region", e.target.value)
                    }
                    placeholder={t("stations.region")}
                  />
                </div>

                <div className="form-group">
                  <label>{t("stations.phone")}</label>
                  <input
                    type="text"
                    value={stationForm.phone}
                    onChange={(e) =>
                      handleStationFormChange("phone", e.target.value)
                    }
                    placeholder={t("stations.phone")}
                  />
                </div>

                <div style={s.formRow}>
                  <div className="form-group" style={{ flex: 1 }}>
                    <label>{t("stations.latitude")}</label>
                    <input
                      type="number"
                      step="any"
                      value={stationForm.latitude}
                      onChange={(e) =>
                        handleStationFormChange("latitude", e.target.value)
                      }
                      placeholder="41.2995"
                    />
                  </div>
                  <div className="form-group" style={{ flex: 1 }}>
                    <label>{t("stations.longitude")}</label>
                    <input
                      type="number"
                      step="any"
                      value={stationForm.longitude}
                      onChange={(e) =>
                        handleStationFormChange("longitude", e.target.value)
                      }
                      placeholder="69.2401"
                    />
                  </div>
                </div>
              </div>

              <div style={s.modalFooter}>
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={closeStationModal}
                  disabled={stationSaving}
                >
                  {t("common.cancel")}
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={stationSaving}
                >
                  {stationSaving ? (
                    <>
                      <span className="spinner" />
                      {t("common.loading")}
                    </>
                  ) : (
                    t("common.save")
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {columnModalOpen && (
        <div style={s.overlay} onClick={closeColumnModal}>
          <div style={s.modal} onClick={(e) => e.stopPropagation()}>
            <div style={s.modalHeader}>
              <h2 style={s.modalTitle}>
                {editingColumn
                  ? t("stations.editColumn")
                  : t("stations.addColumn")}
              </h2>
              <button style={s.modalCloseBtn} onClick={closeColumnModal}>
                &times;
              </button>
            </div>
            <form onSubmit={handleColumnSubmit}>
              {columnFormError && (
                <div style={s.formError}>{columnFormError}</div>
              )}

              <div style={s.formBody}>
                <div className="form-group">
                  <label>{t("stations.columnName")} *</label>
                  <input
                    type="text"
                    value={columnForm.name}
                    onChange={(e) =>
                      handleColumnFormChange("name", e.target.value)
                    }
                    placeholder={t("stations.columnName")}
                  />
                </div>

                <div className="form-group">
                  <label>{t("stations.fuelType")} *</label>
                  <select
                    value={columnForm.fuelType}
                    onChange={(e) =>
                      handleColumnFormChange("fuelType", e.target.value)
                    }
                  >
                    <option value="">
                      -- {t("stations.selectFuelType")} --
                    </option>
                    {FUEL_TYPES.map((ft) => (
                      <option key={ft.value} value={ft.value}>
                        {t("stations.fuelTypes." + ft.key)}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label>{t("stations.pricePerLiter")}</label>
                  <input
                    type="number"
                    step="any"
                    value={columnForm.pricePerLiter}
                    onChange={(e) =>
                      handleColumnFormChange("pricePerLiter", e.target.value)
                    }
                    placeholder="0.00"
                  />
                </div>

                <div className="form-group">
                  <label>{t("stations.columnNumber")}</label>
                  <input
                    type="number"
                    value={columnForm.columnNumber}
                    onChange={(e) =>
                      handleColumnFormChange("columnNumber", e.target.value)
                    }
                    placeholder="1"
                  />
                </div>
              </div>

              <div style={s.modalFooter}>
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={closeColumnModal}
                  disabled={columnSaving}
                >
                  {t("common.cancel")}
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={columnSaving}
                >
                  {columnSaving ? (
                    <>
                      <span className="spinner" />
                      {t("common.loading")}
                    </>
                  ) : (
                    t("common.save")
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteModalOpen && deleteTarget && (
        <div style={s.overlay} onClick={closeDeleteModal}>
          <div
            style={{ ...s.modal, maxWidth: "420px" }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={s.modalHeader}>
              <h2 style={s.modalTitle}>
                {deleteTarget.type === "station"
                  ? t("stations.deleteStation")
                  : t("stations.deleteColumn")}
              </h2>
              <button style={s.modalCloseBtn} onClick={closeDeleteModal}>
                &times;
              </button>
            </div>
            <div style={s.deleteBody}>
              <div style={s.deleteIcon}>{String.fromCodePoint(0x26a0)}</div>
              <p style={s.deleteText}>
                {deleteTarget.type === "station"
                  ? t("stations.deleteConfirm")
                  : t("stations.confirmDeleteColumn")}
              </p>
              {deleteTarget.name && (
                <p style={s.deleteTargetName}>
                  &ldquo;{deleteTarget.name}&rdquo;
                </p>
              )}
            </div>
            <div style={s.modalFooter}>
              <button
                className="btn btn-outline"
                onClick={closeDeleteModal}
                disabled={deleteLoading}
              >
                {t("common.cancel")}
              </button>
              <button
                className="btn btn-danger"
                onClick={handleDeleteConfirm}
                disabled={deleteLoading}
              >
                {deleteLoading ? (
                  <>
                    <span className="spinner" />
                    {t("common.loading")}
                  </>
                ) : (
                  t("common.delete")
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

const s = {
  container: {
    position: "relative",
  },

  toastContainer: {
    position: "fixed",
    top: "16px",
    right: "16px",
    zIndex: 1100,
    display: "flex",
    flexDirection: "column",
    gap: "8px",
  },
  toast: {
    padding: "12px 16px",
    borderRadius: "8px",
    fontSize: "0.875rem",
    fontWeight: 500,
    color: "#fff",
    boxShadow: "0 2px 8px rgba(0,0,0,0.3)",
    animation: "toastIn 0.3s ease",
    maxWidth: "380px",
    wordBreak: "break-word",
  },
  toastSuccess: {
    background: "#238636",
  },
  toastError: {
    background: "#da3633",
  },

  columnsBadge: {
    display: "inline-flex",
    alignItems: "center",
    justifyContent: "center",
    minWidth: "28px",
    padding: "2px 8px",
    borderRadius: "12px",
    fontSize: "0.8rem",
    fontWeight: 600,
    background: "rgba(88,166,255,0.12)",
    color: "#58a6ff",
    transition: "all 0.2s ease",
  },
  columnsBadgeExpanded: {
    background: "rgba(88,166,255,0.25)",
  },

  expandedCell: {
    padding: 0,
    background: "#0d1117",
    borderBottom: "1px solid #30363d",
  },
  columnsSection: {
    padding: "16px 20px",
    background: "#0d1117",
  },
  columnsTitle: {
    fontSize: "0.95rem",
    fontWeight: 600,
    color: "#c9d1d9",
    margin: 0,
  },
  columnsTable: {
    width: "100%",
    borderCollapse: "collapse",
    fontSize: "0.85rem",
    border: "1px solid #21262d",
    borderRadius: "6px",
    overflow: "hidden",
  },

  fuelBadge: {
    display: "inline-block",
    padding: "2px 8px",
    borderRadius: "4px",
    fontSize: "0.78rem",
    fontWeight: 600,
    background: "rgba(163,113,247,0.15)",
    color: "#a371f7",
  },

  overlay: {
    position: "fixed",
    inset: 0,
    background: "rgba(0,0,0,0.6)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    zIndex: 1000,
    padding: "24px",
  },
  modal: {
    width: "100%",
    maxWidth: "520px",
    maxHeight: "90vh",
    overflowY: "auto",
    background: "#161b22",
    border: "1px solid #30363d",
    borderRadius: "12px",
    boxShadow: "0 12px 40px rgba(0,0,0,0.5)",
  },
  modalHeader: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    padding: "18px 24px",
    borderBottom: "1px solid #30363d",
  },
  modalTitle: {
    fontSize: "1.05rem",
    fontWeight: 600,
    color: "#c9d1d9",
    margin: 0,
  },
  modalCloseBtn: {
    background: "none",
    border: "none",
    color: "#8b949e",
    fontSize: "1.5rem",
    cursor: "pointer",
    padding: "0 4px",
    lineHeight: 1,
    transition: "color 0.2s ease",
  },
  modalFooter: {
    display: "flex",
    justifyContent: "flex-end",
    gap: "8px",
    padding: "16px 24px",
    borderTop: "1px solid #30363d",
  },
  formError: {
    background: "rgba(248,81,73,0.1)",
    border: "1px solid rgba(248,81,73,0.3)",
    borderRadius: "6px",
    padding: "10px 14px",
    color: "#f85149",
    fontSize: "0.85rem",
    margin: "16px 24px 0",
  },
  formRow: {
    display: "flex",
    gap: "12px",
  },
  formBody: {
    padding: "20px 24px",
  },

  deleteBody: {
    padding: "24px",
    textAlign: "center",
  },
  deleteIcon: {
    fontSize: "2.5rem",
    marginBottom: "12px",
    color: "#f85149",
  },
  deleteText: {
    fontSize: "0.95rem",
    color: "#c9d1d9",
    margin: "0 0 8px",
    lineHeight: 1.5,
  },
  deleteTargetName: {
    fontSize: "1rem",
    fontWeight: 600,
    color: "#58a6ff",
    margin: 0,
  },
};
