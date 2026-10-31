window.updateFontSize = (className, step) => {
    const rootFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize); // Default 16px
    let cssVarName = "";

    // Determine which CSS variable to modify
    if (className === "special-keypad-button") {
        cssVarName = "--dynamic-keypad-btn-size";
    } else if (className === "special-icon-btn") {
        cssVarName = "--dynamic-icon-size";
    } else if (className === "special-quantity-button") {
        cssVarName = "--dynamic-quantity-btn-size";
    } else if (className === "finance-label-text-size") {
        cssVarName = "--dynamic-finance-label-text-size";
    } else if (className === "finance-input-text-size") {
        cssVarName = "--dynamic-finance-input-text-size";
    } else if (className === "special-sec4-button") {
        cssVarName = "--dynamic-sec4-btn-size";
    } else {
        console.warn(`No variable associated with class: ${className}`);
        return;
    }

    // Get current font size from CSS variable
    let currentSizeRem = parseFloat(getComputedStyle(document.documentElement).getPropertyValue(cssVarName)) || 1.25;

    const stepRem = step / rootFontSize; // Convert step from px to rem
    const newFontSizeRem = currentSizeRem + stepRem; // Adjust font size in rem

    // Update the CSS variable
    document.documentElement.style.setProperty(cssVarName, `${newFontSizeRem}rem`);

    console.log(`Updated ${className} Font Size: ${newFontSizeRem}rem`);

    // Store updated font size in localStorage
    localStorage.setItem(`fontSize_${className}`, newFontSizeRem);
};

// Restore font sizes from localStorage on page load
document.addEventListener("DOMContentLoaded", () => {
    loadStoredFontSizes();
});

function loadStoredFontSizes() {
    const keypadBtnSize = localStorage.getItem("fontSize_special-keypad-button");
    const quantityBtnSize = localStorage.getItem("fontSize_special-quantity-button");
    const iconSize = localStorage.getItem("fontSize_special-icon-btn");
    const financeLabelTextSize = localStorage.getItem("fontSize_finance-label-text-size");
    const financeInputTextSize = localStorage.getItem("fontSize_finance-input-text-size");
    const sec4BtnSize = localStorage.getItem("fontSize_special-sec4-button");

    if (keypadBtnSize) {
        document.documentElement.style.setProperty("--dynamic-keypad-btn-size", `${keypadBtnSize}rem`);
    }
    if (iconSize) {
        document.documentElement.style.setProperty("--dynamic-icon-size", `${iconSize}rem`);
    }
    if (quantityBtnSize) {
        document.documentElement.style.setProperty("--dynamic-quantity-btn-size", `${quantityBtnSize}rem`);
    }
    if (financeLabelTextSize) {
        document.documentElement.style.setProperty("--dynamic-finance-label-text-size", `${financeLabelTextSize}rem`);
    }
    if (financeInputTextSize) {
        document.documentElement.style.setProperty("--dynamic-finance-input-text-size", `${financeInputTextSize}rem`);
    }
    if (sec4BtnSize) {
        document.documentElement.style.setProperty("--dynamic-sec4-btn-size", `${sec4BtnSize}rem`);
    }
}

function clearSec4FontSizeStorage() {
    // Remove specific font size keys from local storage
    localStorage.removeItem("fontSize_finance-label-text-size");
    localStorage.removeItem("fontSize_finance-input-text-size");
    localStorage.removeItem("fontSize_special-sec4-button");


    // Reset CSS variables to default values (adjust as needed)
    document.documentElement.style.setProperty("--dynamic-finance-label-text-size", "1.125rem");
    document.documentElement.style.setProperty("--dynamic-sec4-btn-size", "1.125rem");
    document.documentElement.style.setProperty("--dynamic-finance-input-text-size", "1rem");
}

function clearSec3FontSizeStorage() {
    // Remove specific font size keys from local storage
    localStorage.removeItem("fontSize_special-keypad-button");
    localStorage.removeItem("fontSize_special-quantity-button");
    localStorage.removeItem("fontSize_special-icon-btn");

    // Reset CSS variables to default values (adjust as needed)
    document.documentElement.style.setProperty("--dynamic-keypad-btn-size", "1rem");
    document.documentElement.style.setProperty("--dynamic-icon-size", "1.25rem");
    document.documentElement.style.setProperty("--dynamic-quantity-btn-size", "1rem");

    console.log("Font sizes reset and local storage cleared.");
}

function showModal(id) {
    var myModal = new bootstrap.Modal(document.querySelector(id));
    myModal.show();
}

function hideModal(id) {
    var modal = bootstrap.Modal.getInstance(document.querySelector(id));
    modal.hide();
}

window.downloadBase64File = (fileName, contentType, base64Data) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.createBlobUrl = (base64, contentType) => {
    try {
        const byteCharacters = atob(base64);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });
        const url = URL.createObjectURL(blob);
        return url;
    } catch (e) {
        console.error("Error creating blob URL:", e);
        return null;
    }
};

window.createBlobFromUint8Array = (byteArray, contentType) => {
    try {
        const blob = new Blob([byteArray], { type: contentType });
        return URL.createObjectURL(blob);
    } catch (e) {
        console.error("Error creating blob URL from array:", e);
        return null;
    }
};

window.revokeBlobUrl = (url) => {
    if (url && url.startsWith('blob:')) {
        URL.revokeObjectURL(url);
    }
};