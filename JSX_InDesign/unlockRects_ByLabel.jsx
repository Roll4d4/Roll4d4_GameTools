var doc = app.activeDocument;
var labelsToUnlock = ["FiligreeImage", "BackgroundImage"];
var logMessages = "=== START: Unlock Master Page Items by Label ===\n";

for (var i = 0; i < doc.pages.length; i++) {
    var page = doc.pages[i];
    var master = page.appliedMaster;
    logMessages += "\n> Checking Page " + (i + 1) + "\n";

    if (!master) {
        logMessages += "  - No Master Page applied on this page.\n";
        $.writeln("Page " + (i + 1) + " has no master page applied.");
        continue;
    }

    // Get spread to ensure we're placing things correctly
    var spread = page.parent;

    // Debug: List the labels of all items on the applied master
    var masterItems = master.pageItems;
    $.writeln("Page " + (i + 1) + " master: " + master.name);

    for (var j = 0; j < masterItems.length; j++) {
        $.writeln("  Master item " + (j + 1) + " label: '" + masterItems[j].label + "'");
    }

    // Process each label you want to unlock
    for (var k = 0; k < labelsToUnlock.length; k++) {
        var labelToFind = labelsToUnlock[k];
        logMessages += "\n--- Processing Items for Label: " + labelToFind + " ---\n";
        var found = false;

        for (var j = 0; j < masterItems.length; j++) {
            var item = masterItems[j];

            // Compare label to target label
            if (String(item.label) === labelToFind) {
                found = true;
                logMessages += "  - Found " + labelToFind + " on Master Page.\n";
                try {
                    // Ensure we override ONLY if not already overridden
                    if (!item.overridden) {
                        var overriddenItem = item.override(page);
                        overriddenItem.locked = false;
 
                         // Preserve flip if it was horizontally flipped
                        if (flippedHorizontally) {
                            overriddenItem.absoluteHorizontalScale *= -1;
                        }                        
                        
                        overriddenItem.move([item.geometricBounds[1], item.geometricBounds[0]]); // Reset position
                        logMessages += "    -> OVERRIDDEN & UNLOCKED: " + labelToFind + "\n";
                        $.writeln("Unlocked " + labelToFind + " on page " + (i + 1));
                    } else {
                        $.writeln("Skipping duplicate override for " + labelToFind + " on page " + (i + 1));
                    }
                } catch (e) {
                    logMessages += "    -> ERROR: Could not override " + labelToFind + " (" + e + ")\n";
                    $.writeln("Error overriding " + labelToFind + " on page " + (i + 1) + ": " + e);
                }
            }
        }
        if (!found) {
            logMessages += "  - No matching items found on this page for label: " + labelToFind + "\n";
            $.writeln("No " + labelToFind + " found on page " + (i + 1));
        }
    }
}

logMessages += "\n=== FINISHED: All Master Page Items Processed ===\n";
alert(logMessages);
