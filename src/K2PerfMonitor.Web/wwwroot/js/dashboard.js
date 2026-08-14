// Client helpers for K2 Performance Monitor dashboard
window.k2pm = {
    // trigger a file download from a string (used for CSV export)
    downloadFile: function (filename, content, mime) {
        const blob = new Blob([content], { type: mime || 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};
