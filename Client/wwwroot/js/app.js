window.clipboardCopy = {
    copyText: function(text) {
        navigator.clipboard.writeText(text).then(r => {});
    }
};