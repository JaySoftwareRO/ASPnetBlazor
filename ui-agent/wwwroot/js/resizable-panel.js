const minPaneSize = 150;
const maxPaneSize = document.body.clientWidth * .5

// setPaneWidth sets the width of a div
const setPaneWidth = (element, width) => {
    element.style.setProperty('width', `${width}px`);
};

// getPaneWidth gets the width of a div
const getPaneWidth = (element) => {
    const pxWidth = getComputedStyle(element).getPropertyValue('width');
    return parseInt(pxWidth, 10);
};

// setPaneHeight sets the height of a div
const setPaneHeight = (element, height) => {
    element.style.setProperty('height', `${height}px`);
};

// getPaneHeight gets the height of a div
const getPaneHeight = (element) => {
    const pxHeight = getComputedStyle(element).getPropertyValue('height');
    return parseInt(pxHeight, 10);
};

// startDraggingLeft is the event handler for separators that resize the div on their left
const startDraggingLeft = (event) => {
    event.preventDefault();
    const resizableParent = event.target.previousElementSibling;
    const startingPaneWidth = getPaneWidth(resizableParent);
    const xOffset = event.pageX;

    const mouseDragHandler = (moveEvent) => {
        moveEvent.preventDefault();
        const primaryButtonPressed = moveEvent.buttons === 1;

        if (!primaryButtonPressed) {
            var paneWidth = getPaneWidth(resizableParent);
            setPaneWidth(resizableParent, Math.min(Math.max(paneWidth, minPaneSize), maxPaneSize));
            document.body.removeEventListener('pointermove', mouseDragHandler);
            return;
        }
        const paneOriginAdjustment = 'left' === 'right' ? 1 : -1;
        setPaneWidth(resizableParent, (xOffset - moveEvent.pageX) * paneOriginAdjustment + startingPaneWidth);
    };

    document.body.addEventListener('pointermove', mouseDragHandler);
};

// startDraggingLeft is the event handler for separators that resize the div on their right
const startDraggingRight = (event) => {
    event.preventDefault();
    const resizableParent = event.target.nextElementSibling;
    const startingPaneWidth = getPaneWidth(resizableParent);
    const xOffset = event.pageX;

    const mouseDragHandler = (moveEvent) => {
        moveEvent.preventDefault();
        const primaryButtonPressed = moveEvent.buttons === 1;

        if (!primaryButtonPressed) {
            var paneWidth = getPaneWidth(resizableParent);
            setPaneWidth(resizableParent, Math.min(Math.max(paneWidth, minPaneSize), maxPaneSize));
            document.body.removeEventListener('pointermove', mouseDragHandler);
            return;
        }
        const paneOriginAdjustment = 'left' === 'right' ? -1 : +1;
        setPaneWidth(resizableParent, (xOffset - moveEvent.pageX) * paneOriginAdjustment + startingPaneWidth);
    };

    document.body.addEventListener('pointermove', mouseDragHandler);
};

// startDraggingUp is the event handler for separators that resize the div above them
const startDraggingUp = (event) => {
    event.preventDefault();
    const resizableParent = event.target.previousElementSibling;
    const startingPaneHeight = getPaneHeight(resizableParent);
    const yOffset = event.pageY;

    const mouseDragHandler = (moveEvent) => {
        moveEvent.preventDefault();
        const primaryButtonPressed = moveEvent.buttons === 1;

        if (!primaryButtonPressed) {
            var paneHeight = getPaneHeight(resizableParent);
            setPaneHeight(resizableParent, Math.min(Math.max(paneHeight, minPaneSize), maxPaneSize));
            document.body.removeEventListener('pointermove', mouseDragHandler);
            return;
        }
        const paneOriginAdjustment = 'up' === 'down' ? +1 : -1;
        setPaneHeight(resizableParent, (yOffset - moveEvent.pageY) * paneOriginAdjustment + startingPaneHeight);
    };

    document.body.addEventListener('pointermove', mouseDragHandler);
};

// startDraggingDown is the event handler for separators that resize the div below them
const startDraggingDown = (event) => {
    event.preventDefault();
    const resizableParent = event.target.nextElementSibling;
    const startingPaneHeight = getPaneHeight(resizableParent);
    const yOffset = event.pageY;

    const mouseDragHandler = (moveEvent) => {
        moveEvent.preventDefault();
        const primaryButtonPressed = moveEvent.buttons === 1;

        if (!primaryButtonPressed) {
            var paneHeight = getPaneHeight(resizableParent);
            setPaneHeight(resizableParent, Math.min(Math.max(paneHeight, minPaneSize), maxPaneSize));
            document.body.removeEventListener('pointermove', mouseDragHandler);
            return;
        }
        const paneOriginAdjustment = 'up' === 'down' ? -1 : +1;
        setPaneHeight(resizableParent, (yOffset - moveEvent.pageY) * paneOriginAdjustment + startingPaneHeight);
    };

    document.body.addEventListener('pointermove', mouseDragHandler);
};

// Register events for all handles in the document
var handles = document.getElementsByClassName("resizable-left-panel-handle");
for (var i = 0; i < handles.length; i++) {
    var handle = handles[i];
    handle.addEventListener('mousedown', startDraggingLeft);
}

handles = document.getElementsByClassName("resizable-right-panel-handle");
for (var i = 0; i < handles.length; i++) {
    var handle = handles[i];
    handle.addEventListener('mousedown', startDraggingRight);
}

handles = document.getElementsByClassName("resizable-up-panel-handle");
for (var i = 0; i < handles.length; i++) {
    var handle = handles[i];
    handle.addEventListener('mousedown', startDraggingUp);
}

handles = document.getElementsByClassName("resizable-down-panel-handle");
for (var i = 0; i < handles.length; i++) {
    var handle = handles[i];
    handle.addEventListener('mousedown', startDraggingDown);
}
