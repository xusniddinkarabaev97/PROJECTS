import { useState, useEffect } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function MapView() {
  const { t } = useTranslation();
  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getStations()
      .then((res) => setStations(Array.isArray(res) ? res : []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  // Calculate center point
  const validStations = stations.filter((s) => s.latitude && s.longitude);
  const centerLat =
    validStations.length > 0
      ? validStations.reduce((s, st) => s + st.latitude, 0) /
        validStations.length
      : 41.2995; // Tashkent default
  const centerLng =
    validStations.length > 0
      ? validStations.reduce((s, st) => s + st.longitude, 0) /
        validStations.length
      : 69.2401;

  return (
    <div>
      <div className="card-header">
        <h2
          style={{
            fontSize: 24,
            fontWeight: 700,
            color: "var(--text-primary)",
          }}
        >
          🗺️ {t("map")}
        </h2>
        <span style={{ color: "var(--text-secondary)", fontSize: 13 }}>
          {validStations.length} {t("stations")}
        </span>
      </div>

      {loading ? (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: 60,
            gap: 12,
          }}
        >
          <div className="spinner" />
          <span style={{ color: "var(--text-secondary)" }}>
            {t("loading")}
          </span>
        </div>
      ) : (
        <div
          className="card"
          style={{ padding: 0, overflow: "hidden", height: "calc(100vh - 180px)", minHeight: 400 }}
        >
          <iframe
            title="Stations Map"
            style={{ width: "100%", height: "100%", border: "none" }}
            srcDoc={getMapHtml(validStations, centerLat, centerLng, t)}
          />
        </div>
      )}
    </div>
  );
}

function getMapHtml(stations, centerLat, centerLng, t) {
  const markers = stations
    .map(
      (s) => `
    L.marker([${s.latitude}, ${s.longitude}])
      .bindPopup('<b>${escapeHtml(s.name)}</b><br/>${escapeHtml(s.address || "")}<br/>${escapeHtml(s.region || "")}, ${escapeHtml(s.district || "")}')
      .addTo(map);`,
    )
    .join("\n");

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"><\/script>
  <style>
    * { margin:0; padding:0; }
    html,body { height:100%; background:#0a0e14; }
    #map { height:100%; }
    .leaflet-popup-content { font-family: -apple-system,sans-serif; font-size:13px; color:#333; }
  </style>
</head>
<body>
  <div id="map"></div>
  <script>
    var map = L.map('map').setView([${centerLat}, ${centerLng}], ${stations.length <= 1 ? 14 : 7});
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 19
    }).addTo(map);
    ${markers}
    // Add legend
    var legend = L.control({position: 'bottomright'});
    legend.onAdd = function() {
      var div = L.DomUtil.create('div', 'info legend');
      div.style.cssText = 'background:#181f2a;color:#e6edf3;padding:8px 12px;border-radius:8px;font-size:12px;border:1px solid #252d3a;';
      div.innerHTML = '🅿️ <b>${stations.length} ${t("stations")}</b>';
      return div;
    };
    legend.addTo(map);
  <\/script>
</body>
</html>`;
}

function escapeHtml(str) {
  if (!str) return "";
  return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}
