var routeMap = null;
var routeFeatures = null;
function CreateMap() {
    if(routeMap)
        routeMap.remove();
    routeMap = L.map('route-map').setView([0, 0], 0);
    routeFeatures = L.featureGroup();
    markers = [];
    
    let Esri_WorldGrayCanvas = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Light_Gray_Base/MapServer/tile/{z}/{y}/{x}', {
        attribution: 'Tiles &copy; Esri &mdash; Esri, DeLorme, NAVTEQ',
        maxZoom: 10,
        minZoom: 3
    });
    let Esri_WorldGrayReference = L.tileLayer('https://services.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Light_Gray_Reference/MapServer/tile/{z}/{y}/{x}', {
        attribution: 'Tiles &copy; Esri &mdash; Esri, DeLorme, NAVTEQ',
        maxZoom: 10,
        minZoom: 3
    });

    Esri_WorldGrayCanvas.addTo(routeMap);
    Esri_WorldGrayReference.addTo(routeMap);
}

function AddWaypoint(lat, long, title, color){
    let marker = L.circleMarker([lat, long], {
        stroke: false,
        color: color,
        fillOpacity: 1,
        radius: 3,
        interactive: false
    });
    marker.bindTooltip(title, {
        permanent: true,
        direction: 'top'
    }).openTooltip();
    markers.push(marker);
    routeFeatures.addLayer(marker);
}

function AddLine(latList, color){
    let line = L.polyline(latList, {
        color: color,
        weight: 1
    });
    routeFeatures.addLayer(line);
}

function ScrollToView(){
    window.setTimeout(function() {
        routeFeatures.addTo(routeMap);
        routeMap.fitBounds(routeFeatures.getBounds());
    }, 10);
}