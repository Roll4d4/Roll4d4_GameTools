#target indesign

    (function () {
        var doc = app.activeDocument;
        var imageFolder = Folder.selectDialog("Select the folder containing the images");

        if (!imageFolder) {
            alert("No folder selected. Exiting.");
            return;
        }

        var images = imageFolder.getFiles(/\.(jpg|jpeg|png|tif|tiff|psd|ai)$/i);
        if (images.length === 0) {
            alert("No valid images found in the folder.");
            return;
        }

        // UI Dialog Setup
        var dlg = new Window("dialog", "Auto Image Placer");
        dlg.orientation = "column";

        dlg.add("statictext", undefined, "Enter Start Page:");
        var startPageInput = dlg.add("edittext", undefined, "1");
        startPageInput.characters = 5;

        dlg.add("statictext", undefined, "Enter End Page (Leave blank for all):");
        var endPageInput = dlg.add("edittext", undefined, "");
        endPageInput.characters = 5;

        dlg.add("statictext", undefined, "Select Label to Fill:");
        var labelDropdown = dlg.add("dropdownlist", undefined, ["FiligreeImage", "BackgroundImage"]);
        labelDropdown.selection = 0;

        dlg.add("statictext", undefined, "Choose Placement Mode:");
        var modeDropdown = dlg.add("dropdownlist", undefined, ["Single Image", "Sequential", "Random"]);
        modeDropdown.selection = 1; // Default to Sequential

        var chooseFileBtn;
        var selectedFile;

        function toggleFileSelection() {
            chooseFileBtn.enabled = (modeDropdown.selection.index === 0);
        }

        chooseFileBtn = dlg.add("button", undefined, "Select Image (Only for Single Image Mode)");
        chooseFileBtn.enabled = (modeDropdown.selection.index === 0);

        chooseFileBtn.onClick = function () {
            selectedFile = File.openDialog("Select an image file", "*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.psd;*.ai", false);
            if (selectedFile) {
                alert("Selected: " + selectedFile.name);
            }
        };

        modeDropdown.onChange = toggleFileSelection;

        var okButton = dlg.add("button", undefined, "OK");

        okButton.onClick = function () {
            dlg.close(1);
        };

        if (dlg.show() !== 1) {
            return;
        }

        var startPage = parseInt(startPageInput.text, 10) || 1;
        var endPage = parseInt(endPageInput.text, 10) || doc.pages.length;
        var selectedLabel = labelDropdown.selection.text;
        var placementMode = modeDropdown.selection.index;

        if (placementMode === 0 && !selectedFile) {
            alert("You must select an image file for Single Image Mode.");
            return;
        }

        var imageIndex = 0; // Track for sequential placement

        for (var i = startPage - 1; i < endPage && i < doc.pages.length; i++) {
            var page = doc.pages[i];
            var frames = page.pageItems;

            $.writeln("\nChecking Page " + (i + 1));
            $.writeln("Looking for frames with label: " + selectedLabel);

            var foundFrame = false;

            for (var j = 0; j < frames.length; j++) {
                var frame = frames[j];

                // Debugging: Print all labels found on this page
                $.writeln(" - Found item label: '" + frame.label + "' (" + frame.constructor.name + ")");

                // If it's a PageItem, try casting it to a rectangle
                if (frame.constructor.name === "PageItem") {
                    try {
                        frame = frame.getElements()[0]; // Get the actual element inside
                        $.writeln("   -> Converted PageItem to " + frame.constructor.name);
                    } catch (e) {
                        $.writeln("   ❌ Could not convert PageItem.");
                        continue;
                    }
                }

                // Now check if it's the selected label and a valid frame
                if ((frame.constructor.name === "Rectangle" || frame.constructor.name === "GraphicFrame") &&
                    String(frame.label) === selectedLabel && !frame.locked) {

                    foundFrame = true;

                    var imageFile;
                    if (placementMode === 0) { // Single Image Mode
                        imageFile = selectedFile;
                    } else if (placementMode === 1) { // Sequential Mode
                        imageFile = images[imageIndex % images.length];
                        imageIndex++; // Move to next image
                    } else { // Random Mode
                        imageFile = images[Math.floor(Math.random() * images.length)];
                    }

                    if (imageFile && imageFile.exists) {
                        try {
                            frame.place(imageFile);
                            frame.fit(FitOptions.PROPORTIONALLY);
                            frame.fit(FitOptions.CENTER_CONTENT);
                            $.writeln(" ✅ Placed " + imageFile.name + " in frame on page " + (i + 1));
                        } catch (e) {
                            $.writeln(" ❌ ERROR placing image on page " + (i + 1) + ": " + e);
                        }
                    } else {
                        $.writeln(" ⚠️ No valid image found for page " + (i + 1));
                    }
                }
            }

            if (!foundFrame) {
                $.writeln(" ❌ No frames found with label '" + selectedLabel + "' on page " + (i + 1));
            }
        }

        alert("Images placed successfully! Check the console for details.");
    })();
