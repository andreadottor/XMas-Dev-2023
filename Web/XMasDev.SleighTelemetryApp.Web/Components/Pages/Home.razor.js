

export function initMap(el, lat, lon) {
    var map = L.map(el).setView([lat, lon], 13);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    var santaIcon = L.icon({ iconUrl: 'images/santa claus_icon.png', iconSize: [48, 48], iconAnchor: [24, 48] });

    var marker = L.marker([lat, lon], { icon: santaIcon });
    marker.addTo(map);

    window.santaMarker = marker;

    return map;
}

export function createPolyline(map, lat, lon) {
    var myPolyline = L.polyline([
        [lat, lon],
        [lat, lon]
    ], { color: '#D6001C', weight: 5 });
    myPolyline.addTo(map);
    return myPolyline;
};

export function updateMap(map, polyline, lat, lon) {
    polyline.addLatLng([lat, lon]);

    window.santaMarker.setLatLng([lat, lon]);

    map.panTo([lat, lon]).update();


}