const minPaneSize = 150;
const maxPaneSize = document.body.clientWidth * .5

const setPaneWidth = (element, width) => {
    element.style.setProperty('width', `${width}px`);
};

const getPaneWidth = (element) => {
    const pxWidth = getComputedStyle(element).getPropertyValue('width');
    return parseInt(pxWidth, 10);
};

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

// Register an event for all handles in the document
handles = document.getElementsByClassName("resizable-left-panel-handle");
for (var i = 0; i < handles.length; i++) {
    var handle = handles[i];
    handle.addEventListener('mousedown', startDraggingLeft);
}

handles = document.getElementsByClassName("resizable-right-panel-handle");
for (var i = 0; i < handles.length; i++) {
    var handle = handles[i];
    handle.addEventListener('mousedown', startDraggingRight);
}
