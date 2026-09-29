(function(){"use strict";try{if(typeof document<"u"){var e=document.createElement("style");e.appendChild(document.createTextNode("@keyframes t-spin{0%{transform:rotate(0)}to{transform:rotate(360deg)}}.t-icon{display:inline-block;vertical-align:middle;width:1em;height:1em}.t-icon:before{font-family:unset}.t-icon-loading{animation:t-spin 1s linear infinite}.t-icon.t-size-s,i.t-size-s{font-size:14px}.t-icon.t-size-m,i.t-size-m{font-size:16px}.t-icon.t-size-l,i.t-size-l{font-size:18px}.file-diff[data-v-211ce45d]{margin-top:.65rem;border:1px solid var(--td-component-border);border-radius:10px;overflow:hidden}.file-diff summary[data-v-211ce45d]{cursor:pointer;padding:.55rem .7rem;font-size:12px;font-weight:600;background:var(--td-bg-color-secondarycontainer)}.file-diff pre[data-v-211ce45d]{max-height:320px;overflow:auto;margin:0;padding:.65rem;background:#151922;color:#d7dce5;font-size:12px}.file-diff code span[data-v-211ce45d]{display:block;min-height:1.35em}.diff-add[data-v-211ce45d]{background:#2ea04338;color:#aff5b4}.diff-remove[data-v-211ce45d]{background:#f8514933;color:#ffdcd7}.block[data-v-211ce45d]{display:block}.hidden[data-v-211ce45d]{display:none}.border[data-v-211ce45d]{border-width:1px}.markdown-message[data-v-a575bfbb]{line-height:1.72;overflow-wrap:anywhere}.markdown-message[data-v-a575bfbb] p{margin:0 0 .45rem}.markdown-message[data-v-a575bfbb] p:last-child{margin-bottom:0}.markdown-message[data-v-a575bfbb] h1,.markdown-message[data-v-a575bfbb] h2,.markdown-message[data-v-a575bfbb] h3{margin:.8rem 0 .35rem;line-height:1.3}.markdown-message[data-v-a575bfbb] h1{font-size:1.2rem}.markdown-message[data-v-a575bfbb] h2{font-size:1.08rem}.markdown-message[data-v-a575bfbb] h3{font-size:1rem}.markdown-message[data-v-a575bfbb] ul{margin:.35rem 0;padding-left:1.35rem}.markdown-message[data-v-a575bfbb] code{font-family:ui-monospace,SFMono-Regular,Menlo,monospace;background:var(--td-bg-color-secondarycontainer);padding:.12rem .32rem;border-radius:5px}.markdown-message[data-v-a575bfbb] pre{overflow:auto;margin:.55rem 0;padding:.8rem;border-radius:10px;background:#151922;color:#e5e7eb}.markdown-message[data-v-a575bfbb] pre code{padding:0;background:transparent;white-space:pre}.markdown-message[data-v-a575bfbb] blockquote{margin:.5rem 0;padding-left:.75rem;border-left:3px solid var(--td-brand-color);color:var(--td-text-color-secondary)}.markdown-message[data-v-a575bfbb] .md-gap{height:.45rem}.block[data-v-a575bfbb]{display:block}.ai-workspace[data-v-b7b8845e]{display:flex;flex-direction:column;min-height:620px;height:calc(100vh - 132px);color:var(--td-text-color-primary);background:var(--td-bg-color-container);border:1px solid var(--td-component-border);border-radius:18px;overflow:hidden;box-shadow:var(--td-shadow-1)}.ai-workspace.compact[data-v-b7b8845e]{min-height:520px;height:76vh;border:0;box-shadow:none}.ai-header[data-v-b7b8845e]{display:flex;align-items:center;justify-content:space-between;gap:1rem;padding:1rem 1.25rem;border-bottom:1px solid var(--td-component-border);background:linear-gradient(135deg,color-mix(in srgb,var(--td-brand-color) 10%,var(--td-bg-color-container)),var(--td-bg-color-container))}.ai-title-row[data-v-b7b8845e]{display:flex;align-items:center;gap:.65rem}.ai-header h1[data-v-b7b8845e]{margin:0;font-size:1.2rem}.ai-header p[data-v-b7b8845e]{margin:.3rem 0 0;color:var(--td-text-color-secondary);font-size:12px}.instance-badge[data-v-b7b8845e]{padding:.18rem .48rem;border-radius:999px;color:var(--td-brand-color);background:color-mix(in srgb,var(--td-brand-color) 12%,transparent);font-size:11px;font-weight:600}.header-actions[data-v-b7b8845e],.inline-actions[data-v-b7b8845e]{display:flex;align-items:center;gap:.5rem;flex-wrap:wrap}.message-list[data-v-b7b8845e]{flex:1;overflow-y:auto;padding:1.2rem clamp(.8rem,3vw,3rem);background:color-mix(in srgb,var(--td-bg-color-page) 55%,var(--td-bg-color-container))}.empty-state[data-v-b7b8845e]{min-height:100%;display:grid;place-content:center;justify-items:center;text-align:center;color:var(--td-text-color-secondary)}.empty-state h2[data-v-b7b8845e]{margin:.4rem 0;color:var(--td-text-color-primary)}.empty-state p[data-v-b7b8845e]{max-width:560px;margin:0;line-height:1.7}.empty-mark[data-v-b7b8845e]{display:grid;place-content:center;width:62px;height:62px;border-radius:20px;background:color-mix(in srgb,var(--td-brand-color) 13%,transparent);color:var(--td-brand-color);font-size:30px}.context-hint[data-v-b7b8845e]{margin-top:1rem;padding:.45rem .7rem;border:1px solid var(--td-component-border);border-radius:999px;font-size:12px}.message-row[data-v-b7b8845e]{display:flex;margin:.8rem 0}.message-row.user[data-v-b7b8845e]{justify-content:flex-end}.message-bubble[data-v-b7b8845e]{max-width:min(780px,86%);border-radius:14px;padding:.75rem .9rem}.user-bubble[data-v-b7b8845e]{white-space:pre-wrap;color:var(--td-text-color-anti);background:var(--td-brand-color);border-bottom-right-radius:5px}.assistant-bubble[data-v-b7b8845e]{background:var(--td-bg-color-container);border:1px solid var(--td-component-border);border-bottom-left-radius:5px}.reasoning[data-v-b7b8845e]{margin-bottom:.7rem;color:var(--td-text-color-secondary);font-size:12px}.reasoning summary[data-v-b7b8845e]{cursor:pointer;font-weight:600}.reasoning pre[data-v-b7b8845e]{white-space:pre-wrap;max-height:220px;overflow:auto;margin:.45rem 0 0;padding:.55rem;background:var(--td-bg-color-secondarycontainer);border-radius:8px}.typing[data-v-b7b8845e]{display:flex;gap:4px;padding:.35rem}.typing span[data-v-b7b8845e]{width:7px;height:7px;border-radius:50%;background:var(--td-brand-color);animation:pulse-b7b8845e 1s infinite alternate}.typing span[data-v-b7b8845e]:nth-child(2){animation-delay:.2s}.typing span[data-v-b7b8845e]:nth-child(3){animation-delay:.4s}@keyframes pulse-b7b8845e{to{opacity:.25;transform:translateY(-3px)}}.tool-card[data-v-b7b8845e]{width:min(780px,90%);padding:.72rem .8rem;border:1px solid var(--td-component-border);border-radius:12px;background:var(--td-bg-color-container)}.tool-card.failed[data-v-b7b8845e]{border-color:color-mix(in srgb,var(--td-error-color) 45%,var(--td-component-border))}.tool-title[data-v-b7b8845e]{display:flex;align-items:center;gap:.45rem}.tool-title small[data-v-b7b8845e]{margin-left:auto;color:var(--td-text-color-placeholder)}.approval-box[data-v-b7b8845e],.question-box[data-v-b7b8845e]{margin-top:.65rem;padding:.7rem;border-radius:10px;background:var(--td-bg-color-secondarycontainer)}.approval-box p[data-v-b7b8845e],.question-box p[data-v-b7b8845e]{margin:0 0 .55rem;font-weight:600}.approval-box pre[data-v-b7b8845e],.tool-receipt pre[data-v-b7b8845e]{overflow:auto;max-height:240px;white-space:pre-wrap;margin:0 0 .65rem;padding:.6rem;border-radius:8px;color:#e5e7eb;background:#151922}.option-list[data-v-b7b8845e]{display:flex;flex-wrap:wrap;gap:.45rem}.custom-answer[data-v-b7b8845e]{display:flex;gap:.5rem;margin-top:.55rem}.custom-answer input[data-v-b7b8845e]{flex:1}.tool-receipt[data-v-b7b8845e]{margin-top:.5rem;font-size:12px}.tool-receipt summary[data-v-b7b8845e]{cursor:pointer;color:var(--td-text-color-secondary)}.error-bubble[data-v-b7b8845e],.error-strip[data-v-b7b8845e]{color:var(--td-error-color);background:color-mix(in srgb,var(--td-error-color) 9%,var(--td-bg-color-container));border:1px solid color-mix(in srgb,var(--td-error-color) 28%,transparent)}.error-bubble[data-v-b7b8845e]{max-width:min(780px,90%);padding:.7rem .8rem;border-radius:12px}.activity-strip[data-v-b7b8845e],.error-strip[data-v-b7b8845e]{padding:.45rem 1.25rem;font-size:12px}.activity-strip[data-v-b7b8845e]{display:flex;justify-content:space-between;color:var(--td-brand-color);border-top:1px solid var(--td-component-border)}.composer[data-v-b7b8845e]{padding:.75rem 1rem 1rem;border-top:1px solid var(--td-component-border);background:var(--td-bg-color-container)}.composer-controls[data-v-b7b8845e]{display:flex;gap:.75rem;margin-bottom:.55rem}.composer-controls label[data-v-b7b8845e]{display:flex;align-items:center;gap:.4rem;font-size:12px;color:var(--td-text-color-secondary)}.composer-controls .model-select[data-v-b7b8845e]{flex:1}.composer-controls select[data-v-b7b8845e]{min-width:130px}.model-select select[data-v-b7b8845e]{width:min(420px,100%)}.input-shell[data-v-b7b8845e]{border:1px solid var(--td-component-border);border-radius:13px;overflow:hidden;transition:border-color .2s}.input-shell[data-v-b7b8845e]:focus-within{border-color:var(--td-brand-color)}.input-shell textarea[data-v-b7b8845e]{width:100%;box-sizing:border-box;resize:none;border:0;outline:0;padding:.75rem;color:var(--td-text-color-primary);background:transparent;font:inherit}.input-footer[data-v-b7b8845e]{display:flex;align-items:center;justify-content:space-between;padding:.4rem .55rem .5rem;color:var(--td-text-color-placeholder);font-size:11px}.panel-view[data-v-b7b8845e]{flex:1;overflow-y:auto;padding:1.2rem;background:var(--td-bg-color-page)}.panel-heading[data-v-b7b8845e],.section-heading[data-v-b7b8845e],.settings-card[data-v-b7b8845e],.model-card[data-v-b7b8845e]{display:flex;justify-content:space-between;align-items:center;gap:1rem}.panel-heading[data-v-b7b8845e]{padding-bottom:1rem;border-bottom:1px solid var(--td-component-border)}.panel-heading h2[data-v-b7b8845e],.section-heading h3[data-v-b7b8845e],.settings-card h3[data-v-b7b8845e]{margin:0}.panel-heading p[data-v-b7b8845e],.section-heading p[data-v-b7b8845e],.settings-card p[data-v-b7b8845e]{margin:.25rem 0 0;color:var(--td-text-color-secondary);font-size:12px}.history-list[data-v-b7b8845e],.model-grid[data-v-b7b8845e]{display:grid;gap:.65rem;margin-top:1rem}.history-item[data-v-b7b8845e]{display:flex;align-items:center;gap:.7rem;padding:.75rem;border:1px solid var(--td-component-border);border-radius:12px;background:var(--td-bg-color-container)}.history-item button[data-v-b7b8845e]{display:grid;flex:1;gap:.25rem;text-align:left;border:0;color:inherit;background:transparent;cursor:pointer}.history-item button span[data-v-b7b8845e]{color:var(--td-text-color-secondary);font-size:12px}.settings-card[data-v-b7b8845e],.model-section[data-v-b7b8845e]{margin-top:1rem;padding:1rem;border:1px solid var(--td-component-border);border-radius:14px;background:var(--td-bg-color-container)}.settings-action[data-v-b7b8845e]{display:flex;align-items:center;gap:.65rem}.model-section[data-v-b7b8845e]{display:block}.model-card[data-v-b7b8845e]{padding:.75rem;border:1px solid var(--td-component-border);border-radius:11px}.model-card>div[data-v-b7b8845e]:first-child{display:grid;gap:.2rem}.model-card span[data-v-b7b8845e],.model-card small[data-v-b7b8845e]{color:var(--td-text-color-secondary)}.model-card small[data-v-b7b8845e]{overflow-wrap:anywhere}.muted-box[data-v-b7b8845e],.center-state[data-v-b7b8845e]{display:flex;align-items:center;justify-content:center;gap:.5rem;min-height:100px;color:var(--td-text-color-secondary)}.muted-box[data-v-b7b8845e]{min-height:auto;margin-top:.8rem;padding:1rem;border-radius:10px;background:var(--td-bg-color-secondarycontainer)}.model-form[data-v-b7b8845e]{display:grid;gap:.8rem}.model-form label[data-v-b7b8845e]{display:grid;gap:.3rem;font-size:13px}.model-form label>span[data-v-b7b8845e]{font-weight:600}.model-form input[data-v-b7b8845e],.model-form select[data-v-b7b8845e],.custom-answer input[data-v-b7b8845e],.composer-controls select[data-v-b7b8845e]{box-sizing:border-box;min-height:34px;padding:.42rem .55rem;border:1px solid var(--td-component-border);border-radius:7px;outline:0;color:var(--td-text-color-primary);background:var(--td-bg-color-container)}.model-form input[data-v-b7b8845e]:focus,.model-form select[data-v-b7b8845e]:focus,.custom-answer input[data-v-b7b8845e]:focus,.composer-controls select[data-v-b7b8845e]:focus{border-color:var(--td-brand-color)}.model-form .check-line[data-v-b7b8845e]{display:flex;grid-template-columns:auto 1fr;align-items:center;justify-content:start}.check-line input[data-v-b7b8845e]{min-height:auto}.form-grid[data-v-b7b8845e]{display:grid;grid-template-columns:1fr 1fr;gap:.75rem}.dialog-actions[data-v-b7b8845e]{display:flex;justify-content:flex-end;gap:.6rem;margin-top:.3rem}@media(max-width:720px){.ai-workspace[data-v-b7b8845e]{height:calc(100vh - 100px);min-height:520px}.ai-header[data-v-b7b8845e]{align-items:flex-start}.header-actions[data-v-b7b8845e]{justify-content:flex-end}.message-bubble[data-v-b7b8845e],.tool-card[data-v-b7b8845e]{max-width:96%;width:auto}.composer-controls[data-v-b7b8845e]{align-items:stretch;flex-direction:column}.composer-controls label[data-v-b7b8845e]{justify-content:space-between}.composer-controls select[data-v-b7b8845e],.model-select select[data-v-b7b8845e]{width:68%}.panel-heading[data-v-b7b8845e],.section-heading[data-v-b7b8845e],.settings-card[data-v-b7b8845e],.model-card[data-v-b7b8845e]{align-items:flex-start;flex-direction:column}.form-grid[data-v-b7b8845e]{grid-template-columns:1fr}}.grid[data-v-b7b8845e]{display:grid}.block[data-v-b7b8845e]{display:block}.hidden[data-v-b7b8845e]{display:none}.h1[data-v-b7b8845e]{height:.25rem}.h2[data-v-b7b8845e]{height:.5rem}.h3[data-v-b7b8845e]{height:.75rem}.flex[data-v-b7b8845e]{display:flex}.flex-wrap[data-v-b7b8845e]{flex-wrap:wrap}.transform[data-v-b7b8845e]{transform:translate(var(--un-translate-x)) translateY(var(--un-translate-y)) translateZ(var(--un-translate-z)) rotate(var(--un-rotate)) rotateX(var(--un-rotate-x)) rotateY(var(--un-rotate-y)) rotate(var(--un-rotate-z)) skew(var(--un-skew-x)) skewY(var(--un-skew-y)) scaleX(var(--un-scale-x)) scaleY(var(--un-scale-y)) scaleZ(var(--un-scale-z))}.resize[data-v-b7b8845e]{resize:both}.border[data-v-b7b8845e]{border-width:1px}.outline[data-v-b7b8845e]{outline-style:solid}.transition[data-v-b7b8845e]{transition-property:color,background-color,border-color,text-decoration-color,fill,stroke,opacity,box-shadow,transform,filter,backdrop-filter;transition-timing-function:cubic-bezier(.4,0,.2,1);transition-duration:.15s}.instance-ai-sidebar[data-v-1a108f18]{height:100%;min-height:0}.instance-ai-sidebar[data-v-1a108f18] .ai-workspace{height:100%;min-height:0;border-radius:0}.elements-ai-drawer .t-drawer__body{padding:0;overflow:hidden}.elements-ai-header-button{flex:0 0 auto}.visible[data-v-1a108f18]{visibility:visible}.hidden[data-v-1a108f18]{display:none}.flex[data-v-1a108f18]{display:flex}")),document.head.appendChild(e)}}catch(a){console.error("vite-plugin-css-injected-by-js",a)}})();
function be(t) {
  return t && t.__esModule && Object.prototype.hasOwnProperty.call(t, "default") ? t.default : t;
}
var te, fe;
function ze() {
  return fe || (fe = 1, te = Vue), te;
}
var e = ze(), le, ke;
function Ue() {
  return ke || (ke = 1, le = TDesign), le;
}
var V = Ue();
const je = /* @__PURE__ */ be(V);
function z(t) {
  "@babel/helpers - typeof";
  return z = typeof Symbol == "function" && typeof Symbol.iterator == "symbol" ? function(a) {
    return typeof a;
  } : function(a) {
    return a && typeof Symbol == "function" && a.constructor === Symbol && a !== Symbol.prototype ? "symbol" : typeof a;
  }, z(t);
}
function Ke(t, a) {
  if (z(t) != "object" || !t) return t;
  var i = t[Symbol.toPrimitive];
  if (i !== void 0) {
    var r = i.call(t, a);
    if (z(r) != "object") return r;
    throw new TypeError("@@toPrimitive must return a primitive value.");
  }
  return (a === "string" ? String : Number)(t);
}
function Re(t) {
  var a = Ke(t, "string");
  return z(a) == "symbol" ? a : a + "";
}
function We(t, a, i) {
  return (a = Re(a)) in t ? Object.defineProperty(t, a, {
    value: i,
    enumerable: !0,
    configurable: !0,
    writable: !0
  }) : t[a] = i, t;
}
var ye = (t) => {
  var a = ["strokeLinecap", "fillRule", "clipRule", "strokeWidth"];
  return a.includes(t) ? t.replace(/([a-z0-9]|(?=[A-Z]))([A-Z])/g, "$1-$2").toLowerCase() : t;
}, we = (t, a) => {
  var i = {};
  if (t.attrs)
    for (var [r, y] of Object.entries(t.attrs))
      if (typeof y == "string" && y.startsWith("props.")) {
        var s = y.split(".")[1];
        i[ye(r)] = a[s];
      } else
        i[ye(r)] = y;
  t.tag === "svg" && (i.class = a.class, i.style = a.style, i.onClick = a.onClick);
  var v = t.children ? t.children.map((E) => we(E, a)) : [];
  return e.h(t.tag, i, v);
}, Fe = "t", He = {
  classPrefix: Fe
};
function Xe() {
  var {
    classPrefix: t
  } = He;
  return {
    SIZE: {
      default: "",
      xs: "".concat(t, "-size-xs"),
      small: "".concat(t, "-size-s"),
      medium: "".concat(t, "-size-m"),
      large: "".concat(t, "-size-l"),
      xl: "".concat(t, "-size-xl"),
      block: "".concat(t, "-size-full-width")
    },
    STATUS: {
      loading: "".concat(t, "-is-loading"),
      disabled: "".concat(t, "-is-disabled"),
      focused: "".concat(t, "-is-focused"),
      success: "".concat(t, "-is-success"),
      error: "".concat(t, "-is-error"),
      warning: "".concat(t, "-is-warning"),
      selected: "".concat(t, "-is-selected"),
      active: "".concat(t, "-is-active"),
      checked: "".concat(t, "-is-checked"),
      current: "".concat(t, "-is-current"),
      hidden: "".concat(t, "-is-hidden"),
      visible: "".concat(t, "-is-visible"),
      expanded: "".concat(t, "-is-expanded"),
      indeterminate: "".concat(t, "-is-indeterminate")
    }
  };
}
function Ze(t) {
  var a = Xe().SIZE, i = e.computed(() => t.value in a ? a[t.value] : ""), r = e.computed(() => t.value === void 0 || t.value in a ? {} : {
    fontSize: t.value
  });
  return {
    style: r,
    className: i
  };
}
function he(t, a) {
  var i = Object.keys(t);
  if (Object.getOwnPropertySymbols) {
    var r = Object.getOwnPropertySymbols(t);
    a && (r = r.filter(function(y) {
      return Object.getOwnPropertyDescriptor(t, y).enumerable;
    })), i.push.apply(i, r);
  }
  return i;
}
function ge(t) {
  for (var a = 1; a < arguments.length; a++) {
    var i = arguments[a] != null ? arguments[a] : {};
    a % 2 ? he(Object(i), !0).forEach(function(r) {
      We(t, r, i[r]);
    }) : Object.getOwnPropertyDescriptors ? Object.defineProperties(t, Object.getOwnPropertyDescriptors(i)) : he(Object(i)).forEach(function(r) {
      Object.defineProperty(t, r, Object.getOwnPropertyDescriptor(i, r));
    });
  }
  return t;
}
var Ge = {
  tag: "svg",
  attrs: {
    fill: "none",
    viewBox: "0 0 24 24",
    width: "1em",
    height: "1em"
  },
  children: [{
    tag: "g",
    attrs: {
      id: "chat"
    },
    children: [{
      tag: "path",
      attrs: {
        id: "fill1",
        fill: "props.fillColor1",
        d: "M2.5 3H21.5V17H6.5L2.5 20.5V3Z"
      }
    }, {
      tag: "path",
      attrs: {
        id: "stroke1",
        stroke: "props.strokeColor1",
        d: "M2.5 3H21.5V17H6.5L2.5 20.5V3Z",
        strokeLinecap: "square",
        strokeWidth: "props.strokeWidth"
      }
    }]
  }]
}, Je = e.defineComponent({
  name: "ChatIcon",
  props: {
    size: {
      type: String
    },
    onClick: {
      type: Function
    },
    fillColor: {
      type: [Array, String]
    },
    strokeColor: {
      type: [Array, String]
    },
    strokeWidth: {
      type: Number
    }
  },
  setup(t, a) {
    var {
      attrs: i
    } = a, r = e.computed(() => t.size), y = e.computed(() => t.strokeColor ? Array.isArray(t.strokeColor) ? t.strokeColor[0] : t.strokeColor : "currentColor"), s = e.computed(() => {
      var k;
      return t.strokeColor ? Array.isArray(t.strokeColor) ? (k = t.strokeColor[1]) !== null && k !== void 0 ? k : t.strokeColor[0] : t.strokeColor : "currentColor";
    }), v = e.computed(() => t.fillColor ? Array.isArray(t.fillColor) ? t.fillColor[0] : t.fillColor : "transparent"), E = e.computed(() => {
      var k;
      return t.fillColor ? Array.isArray(t.fillColor) ? (k = t.fillColor[1]) !== null && k !== void 0 ? k : t.fillColor[0] : t.fillColor : "transparent";
    }), u = e.computed(() => t.fillColor ? Array.isArray(t.fillColor) ? t.fillColor[0] : t.fillColor : "currentColor"), {
      className: h,
      style: d
    } = Ze(r), m = e.computed(() => ["t-icon", "t-icon-chat", h.value]), f = e.computed(() => ge(ge({
      fill: "none"
    }, d.value), i.style)), b = e.computed(() => ({
      class: m.value,
      style: f.value,
      onClick: (k) => {
        var g;
        return (g = t.onClick) === null || g === void 0 ? void 0 : g.call(t, {
          e: k
        });
      },
      strokeColor1: y.value,
      strokeColor2: s.value,
      fillColor1: v.value,
      fillColor2: E.value,
      strokeWidth: t.strokeWidth || 2,
      filledColor: u.value
    }));
    return () => we(Ge, b.value);
  }
}), ne, Ee;
function Qe() {
  return Ee || (Ee = 1, ne = mslxRequest), ne;
}
var Ye = Qe();
const x = /* @__PURE__ */ be(Ye), C = "/api/plugins/mslx-plugin-elements-ai/ai";
function et() {
  const t = window.MSLX_Stores;
  return t?.getUserStore?.() || t?.useUserStore?.();
}
function tt() {
  const t = et()?.token;
  if (!t) throw new Error("登录状态已失效，请重新登录。");
  return t;
}
const lt = () => x.get({ url: `${C}/status` }), nt = (t) => x.put({ url: `${C}/preferences`, data: t }), ot = (t) => x.put({ url: `${C}/models`, data: t }), at = (t) => x.delete({ url: `${C}/models/${encodeURIComponent(t)}` }), oe = () => x.get({ url: `${C}/presets` }), rt = (t) => x.put({ url: `${C}/presets`, data: t }), it = (t) => x.delete({ url: `${C}/presets/${encodeURIComponent(t)}` }), ct = () => x.get({ url: `${C}/conversations` }), st = (t) => x.get({ url: `${C}/conversations/${encodeURIComponent(t)}` }), ut = (t) => x.delete({ url: `${C}/conversations`, data: { ids: t } }), dt = (t, a) => x.post({ url: `${C}/approvals/${encodeURIComponent(t)}`, data: { approved: a } }), mt = (t, a) => x.post({ url: `${C}/questions/${encodeURIComponent(t)}`, data: { answer: a } });
async function pt(t, a, i, r, y, s, v) {
  const E = await fetch(`${C}/chat`, {
    method: "POST",
    credentials: "same-origin",
    signal: s,
    headers: {
      "Content-Type": "application/json",
      "X-Requested-With": "XMLHttpRequest",
      "x-user-token": tt()
    },
    body: JSON.stringify({ message: t, conversationId: a, modelId: i, permissionMode: r, currentInstanceId: y })
  });
  if (!E.ok) {
    const b = await E.json().catch(() => ({}));
    throw new Error(b.message || `HTTP ${E.status}`);
  }
  if (!E.body || !E.headers.get("content-type")?.includes("text/event-stream"))
    throw new Error("服务器没有返回事件流。");
  const u = E.body.getReader(), h = new TextDecoder();
  let d = "", m = [], f = !1;
  try {
    for (; !s.aborted; ) {
      const { value: b, done: k } = await u.read();
      if (k) break;
      for (d += h.decode(b, { stream: !0 }); ; ) {
        const g = d.match(/\r?\n/);
        if (!g || g.index === void 0) break;
        const w = d.slice(0, g.index);
        if (d = d.slice(g.index + g[0].length), w)
          w.startsWith("data:") && m.push(w.slice(5).trimStart());
        else if (m.length) {
          const N = JSON.parse(m.join(`
`));
          if (m = [], N.type === "error") throw new Error(N.message);
          v(N), N.type === "done" && (f = !0);
        }
      }
      if (f) break;
    }
    if (!f && !s.aborted) throw new Error("AI 事件流意外中断。");
  } finally {
    await u.cancel().catch(() => {
    }), u.releaseLock();
  }
}
const vt = /* @__PURE__ */ e.defineComponent({
  __name: "FileDiff",
  props: {
    diff: {}
  },
  setup(t) {
    const a = t, i = e.computed(() => a.diff.patch.split(`
`).map((r) => ({
      text: r,
      type: r.startsWith("+") && !r.startsWith("+++") ? "add" : r.startsWith("-") && !r.startsWith("---") ? "remove" : "context"
    })));
    return (r, y) => (e.openBlock(), e.createElementBlock("details", { class: "file-diff" }, [
      e.createElementVNode("summary", null, [
        e.createTextVNode(e.toDisplayString(t.diff.path), 1),
        t.diff.truncated ? (e.openBlock(), e.createElementBlock("span", { key: 0 }, "（已截断）")) : e.createCommentVNode("", !0)
      ]),
      e.createElementVNode("pre", null, [
        e.createElementVNode("code", null, [
          (e.openBlock(!0), e.createElementBlock(e.Fragment, null, e.renderList(i.value, (s, v) => (e.openBlock(), e.createElementBlock("span", {
            key: v,
            class: e.normalizeClass(`diff-${s.type}`)
          }, e.toDisplayString(s.text) + `
`, 3))), 128))
        ])
      ])
    ]));
  }
}), W = (t, a) => {
  const i = t.__vccOpts || t;
  for (const [r, y] of a)
    i[r] = y;
  return i;
}, ft = /* @__PURE__ */ W(vt, [["__scopeId", "data-v-211ce45d"]]), kt = /* @__PURE__ */ e.defineComponent({
  __name: "MarkdownMessage",
  props: {
    content: {}
  },
  setup(t) {
    const a = t;
    function i(s) {
      return s.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
    }
    function r(s) {
      return s.replace(/`([^`\n]+)`/g, "<code>$1</code>").replace(/\*\*([^*\n]+)\*\*/g, "<strong>$1</strong>").replace(new RegExp("(?<!\\*)\\*([^*\\n]+)\\*(?!\\*)", "g"), "<em>$1</em>");
    }
    const y = e.computed(() => {
      const s = [];
      let v = a.content.replace(/```([^\n`]*)\n([\s\S]*?)```/g, (d, m, f) => `@@ELEMENTS_AI_BLOCK_${s.push(
        `<pre><code data-language="${i(String(m).trim())}">${i(String(f))}</code></pre>`
      ) - 1}@@`);
      v = i(v);
      const E = v.split(/\r?\n/), u = [];
      let h = !1;
      for (const d of E) {
        const m = /^@@ELEMENTS_AI_BLOCK_(\d+)@@$/.exec(d);
        if (m) {
          h && u.push("</ul>"), h = !1, u.push(s[Number(m[1])] || "");
          continue;
        }
        const f = /^\s*[-*]\s+(.+)$/.exec(d);
        if (f) {
          h || u.push("<ul>"), h = !0, u.push(`<li>${r(f[1])}</li>`);
          continue;
        }
        h && u.push("</ul>"), h = !1, d.trim() ? d.startsWith("### ") ? u.push(`<h3>${r(d.slice(4))}</h3>`) : d.startsWith("## ") ? u.push(`<h2>${r(d.slice(3))}</h2>`) : d.startsWith("# ") ? u.push(`<h1>${r(d.slice(2))}</h1>`) : d.startsWith("&gt; ") ? u.push(`<blockquote>${r(d.slice(5))}</blockquote>`) : u.push(`<p>${r(d)}</p>`) : u.push('<div class="md-gap"></div>');
      }
      return h && u.push("</ul>"), u.join("");
    });
    return (s, v) => (e.openBlock(), e.createElementBlock("div", {
      class: "markdown-message",
      innerHTML: y.value
    }, null, 8, ["innerHTML"]));
  }
}), yt = /* @__PURE__ */ W(kt, [["__scopeId", "data-v-a575bfbb"]]), ht = /* @__PURE__ */ e.defineComponent({
  __name: "AiWorkspace",
  props: {
    currentInstanceId: {},
    compact: { type: Boolean, default: !1 }
  },
  setup(t) {
    const a = t, i = window.MSLX_Stores, r = i?.getUserStore?.() || i?.useUserStore?.(), y = e.computed(() => r?.token || ""), s = e.ref(), v = e.ref(""), E = e.ref("default"), u = e.ref([]), h = e.ref(), d = e.ref(!0), m = e.ref(""), f = e.ref(!1), b = e.ref(!1), k = e.ref(""), g = e.ref(""), w = e.ref(""), N = e.ref("chat"), U = e.ref(), T = e.ref(), D = e.ref(""), I = e.ref(""), F = e.reactive({}), H = e.ref([]), q = e.ref(!1), S = e.ref([]), L = e.ref([]), X = e.ref(!1), Z = e.ref(!1), A = e.ref(!0), O = e.ref(!1), ae = e.ref("personal"), P = e.ref("default"), p = e.reactive(se()), G = e.ref(!1), J = e.computed(() => s.value?.models || []), re = e.computed(() => J.value.filter((n) => n.source === "personal")), ie = e.computed(
      () => !!(s.value?.ready && v.value && m.value.trim() && !f.value && d.value)
    ), xe = e.computed(() => a.currentInstanceId ? `实例 #${a.currentInstanceId}` : "全局助手"), ce = e.computed(
      () => !!(p.name.trim() && p.endpoint.trim() && p.model.trim())
    );
    function se() {
      return {
        name: "",
        endpoint: "",
        model: "",
        apiKey: "",
        clearApiKey: !1,
        thinkingEnabled: null,
        thinkingEffort: "medium"
      };
    }
    function Q(n = !1) {
      T.value?.abort(), T.value = void 0, u.value = [], h.value = void 0, d.value = !0, f.value = !1, k.value = "", g.value = "", w.value = "", D.value = "", I.value = "", n && (m.value = "");
    }
    function Be() {
      Q(!0), N.value = "chat";
    }
    async function j() {
      b.value = !0, k.value = "";
      try {
        const n = await lt();
        s.value = n, A.value = n.preferences?.sendOnEnter ?? !0, n.models.some((o) => o.id === v.value) || (v.value = n.models[0]?.id || "");
      } catch (n) {
        k.value = B(n);
      } finally {
        b.value = !1;
      }
    }
    async function Y() {
      await e.nextTick(), U.value && (U.value.scrollTop = U.value.scrollHeight);
    }
    function Se(n) {
      if (n.type === "start")
        h.value = n.conversationId, u.value = n.messages;
      else if (n.type === "message") {
        for (; u.value.length <= n.index; )
          u.value.push({ role: "assistant", content: "", pending: !0 });
        u.value[n.index] = n.message;
      } else if (n.type === "delta") {
        const o = u.value[n.index];
        o && (o.content += n.content);
      } else if (n.type === "reasoning") {
        const o = u.value[n.index];
        o && (o.reasoning = (o.reasoning || "") + n.content);
      } else if (n.type === "retry")
        g.value = `模型连接失败，正在进行第 ${n.attempt}/${n.maxAttempts} 次重试…`;
      else if (n.type === "progress") {
        const o = n.progress.value === void 0 ? "" : ` ${Math.round(n.progress.value)}%`, c = n.progress.speed ? ` · ${n.progress.speed}` : "";
        w.value = `${ve(n.tool)}${o}${c}`;
      } else n.type === "done" && (h.value = n.conversationId, g.value = "", w.value = "");
      Y();
    }
    async function ue() {
      if (!ie.value) return;
      const n = m.value.trim();
      m.value = "", f.value = !0, k.value = "", g.value = "", w.value = "";
      const o = new AbortController();
      T.value = o;
      try {
        await pt(
          n,
          h.value,
          v.value,
          E.value,
          a.currentInstanceId,
          o.signal,
          Se
        );
      } catch (c) {
        o.signal.aborted || (k.value = B(c), u.value.push({ role: "error", content: k.value }));
      } finally {
        T.value === o && (T.value = void 0), f.value = !1, g.value = "", w.value = "", await Y();
      }
    }
    function _e() {
      T.value?.abort();
    }
    function $e(n) {
      n.key !== "Enter" || n.isComposing || !(A.value ? !n.shiftKey : n.ctrlKey || n.metaKey) || (n.preventDefault(), ue());
    }
    async function de(n, o) {
      if (!(!n.approval || D.value)) {
        D.value = n.approval.id;
        try {
          await dt(n.approval.id, o);
        } catch (c) {
          V.MessagePlugin.error(B(c));
        } finally {
          D.value = "";
        }
      }
    }
    async function ee(n, o) {
      if (!n.question || I.value) return;
      const c = (o ?? F[n.question.id] ?? "").trim();
      if (c) {
        I.value = n.question.id;
        try {
          await mt(n.question.id, c);
        } catch (_) {
          V.MessagePlugin.error(B(_));
        } finally {
          I.value = "";
        }
      }
    }
    async function me() {
      N.value = "history", q.value = !0, S.value = [];
      try {
        H.value = await ct();
      } catch (n) {
        k.value = B(n);
      } finally {
        q.value = !1;
      }
    }
    async function Me(n) {
      q.value = !0;
      try {
        const o = await st(n);
        u.value = o.messages, h.value = o.id, d.value = o.canContinue, v.value = o.canContinue ? o.modelId : "", N.value = "chat", await Y();
      } catch (o) {
        V.MessagePlugin.error(B(o));
      } finally {
        q.value = !1;
      }
    }
    function Te(n, o) {
      S.value = o ? [.../* @__PURE__ */ new Set([...S.value, n])] : S.value.filter((c) => c !== n);
    }
    function De() {
      if (!S.value.length) return;
      const n = V.DialogPlugin.confirm({
        header: "删除对话",
        body: `确定删除已选择的 ${S.value.length} 个对话吗？`,
        theme: "danger",
        confirmBtn: "删除",
        cancelBtn: "取消",
        onConfirm: async () => {
          try {
            await ut(S.value), h.value && S.value.includes(h.value) && Q(), await me(), V.MessagePlugin.success("对话已删除");
          } catch (o) {
            V.MessagePlugin.error(B(o));
          } finally {
            n.destroy();
          }
        },
        onClose: () => n.destroy(),
        onCancel: () => n.destroy()
      });
    }
    async function Ie() {
      N.value = "settings", X.value = !0;
      try {
        s.value?.admin && (L.value = await oe());
      } catch (n) {
        k.value = B(n);
      } finally {
        X.value = !1;
      }
    }
    async function Ae() {
      if (s.value) {
        Z.value = !0;
        try {
          await nt({ sendOnEnter: A.value }), s.value.preferences.sendOnEnter = A.value, V.MessagePlugin.success("偏好已保存");
        } catch (n) {
          V.MessagePlugin.error(B(n));
        } finally {
          Z.value = !1;
        }
      }
    }
    function K(n, o) {
      ae.value = n, Object.assign(p, se(), o ? {
        id: o.id.split(":", 2)[1],
        name: o.name,
        endpoint: o.endpoint || "",
        model: o.model,
        thinkingEnabled: o.thinkingEnabled,
        thinkingEffort: o.thinkingEffort
      } : {}), P.value = p.thinkingEnabled === null ? "default" : p.thinkingEnabled ? "on" : "off", O.value = !0;
    }
    async function Pe() {
      if (ce.value) {
        G.value = !0, p.thinkingEnabled = P.value === "default" ? null : P.value === "on";
        try {
          ae.value === "preset" ? await rt({ ...p }) : await ot({ ...p }), O.value = !1, await j(), s.value?.admin && (L.value = await oe()), V.MessagePlugin.success("模型已保存");
        } catch (n) {
          V.MessagePlugin.error(B(n));
        } finally {
          G.value = !1;
        }
      }
    }
    function pe(n, o) {
      const c = V.DialogPlugin.confirm({
        header: "删除模型",
        body: `确定删除“${o.name}”吗？`,
        theme: "danger",
        confirmBtn: "删除",
        cancelBtn: "取消",
        onConfirm: async () => {
          try {
            const _ = o.id.split(":", 2)[1];
            n === "preset" ? await it(_) : await at(_), await j(), s.value?.admin && (L.value = await oe()), V.MessagePlugin.success("模型已删除");
          } catch (_) {
            V.MessagePlugin.error(B(_));
          } finally {
            c.destroy();
          }
        },
        onClose: () => c.destroy(),
        onCancel: () => c.destroy()
      });
    }
    function ve(n) {
      return n ? {
        ask_user: "询问用户",
        list_instances: "查询实例",
        get_instance: "读取实例",
        read_terminal: "读取终端",
        control_instance: "控制实例",
        send_command: "发送命令",
        update_instance: "更新实例",
        create_instance: "创建实例",
        delete_instance: "删除实例",
        list_files: "列出文件",
        read_file: "读取文件",
        edit_file: "编辑文件",
        create_file: "创建文件",
        delete_file: "删除文件",
        search_resources: "搜索资源",
        list_resource_versions: "查询资源版本",
        download_resource: "下载资源"
      }[n] || n : "工具";
    }
    function qe(n) {
      return new Intl.DateTimeFormat("zh-CN", { dateStyle: "medium", timeStyle: "short" }).format(n);
    }
    function B(n) {
      return n instanceof Error ? n.message : typeof n == "object" && n && "message" in n ? String(n.message) : "请求失败，请稍后重试。";
    }
    return e.watch(y, (n, o) => {
      n !== o && (Q(!0), s.value = void 0, n && j());
    }), e.onMounted(j), e.onBeforeUnmount(() => T.value?.abort()), (n, o) => {
      const c = e.resolveComponent("t-button"), _ = e.resolveComponent("t-loading"), Le = e.resolveComponent("t-switch"), Oe = e.resolveComponent("t-dialog");
      return e.openBlock(), e.createElementBlock("section", {
        class: e.normalizeClass(["ai-workspace", { compact: t.compact }])
      }, [
        e.createElementVNode("header", { class: "ai-header" }, [
          e.createElementVNode("div", null, [
            e.createElementVNode("div", { class: "ai-title-row" }, [
              e.createElementVNode("h1", null, "Elements AI"),
              e.createElementVNode("span", { class: "instance-badge" }, e.toDisplayString(xe.value), 1)
            ]),
            e.createElementVNode("p", null, "通过安全工具管理 MSLX 实例、终端、文件与模组资源")
          ]),
          e.createElementVNode("div", { class: "header-actions" }, [
            e.createVNode(c, {
              size: "small",
              variant: "outline",
              onClick: Be
            }, {
              default: e.withCtx(() => [
                e.createTextVNode("新对话")
              ]),
              _: 1
            }),
            e.createVNode(c, {
              size: "small",
              variant: "outline",
              onClick: me
            }, {
              default: e.withCtx(() => [
                e.createTextVNode("历史")
              ]),
              _: 1
            }),
            e.createVNode(c, {
              size: "small",
              variant: "outline",
              onClick: Ie
            }, {
              default: e.withCtx(() => [
                e.createTextVNode("设置")
              ]),
              _: 1
            })
          ])
        ]),
        b.value ? (e.openBlock(), e.createElementBlock("div", {
          key: 0,
          class: "center-state"
        }, [
          e.createVNode(_),
          e.createTextVNode(" 正在连接 Elements AI…")
        ])) : N.value === "chat" ? (e.openBlock(), e.createElementBlock(e.Fragment, { key: 1 }, [
          e.createElementVNode("main", {
            ref_key: "list",
            ref: U,
            class: "message-list"
          }, [
            u.value.length ? e.createCommentVNode("", !0) : (e.openBlock(), e.createElementBlock("div", {
              key: 0,
              class: "empty-state"
            }, [
              e.createElementVNode("div", { class: "empty-mark" }, "✦"),
              e.createElementVNode("h2", null, "今天想管理什么？"),
              s.value?.ready ? (e.openBlock(), e.createElementBlock("p", { key: 0 }, "我可以检查实例状态、分析终端、修改配置文件，以及搜索和下载模组。")) : (e.openBlock(), e.createElementBlock("p", { key: 1 }, "尚未配置模型。请先在“设置”中添加个人模型，或让管理员添加预设模型。")),
              t.currentInstanceId ? (e.openBlock(), e.createElementBlock("div", {
                key: 2,
                class: "context-hint"
              }, "当前上下文会优先使用实例 #" + e.toDisplayString(t.currentInstanceId), 1)) : e.createCommentVNode("", !0)
            ])),
            (e.openBlock(!0), e.createElementBlock(e.Fragment, null, e.renderList(u.value, (l, M) => (e.openBlock(), e.createElementBlock("article", {
              key: M,
              class: e.normalizeClass(["message-row", l.role])
            }, [
              l.role === "user" ? (e.openBlock(), e.createElementBlock("div", {
                key: 0,
                class: "message-bubble user-bubble"
              }, e.toDisplayString(l.content), 1)) : l.role === "assistant" ? (e.openBlock(), e.createElementBlock("div", {
                key: 1,
                class: "message-bubble assistant-bubble"
              }, [
                l.reasoning ? (e.openBlock(), e.createElementBlock("details", {
                  key: 0,
                  class: "reasoning",
                  open: l.pending
                }, [
                  e.createElementVNode("summary", null, e.toDisplayString(l.pending ? "正在思考…" : "思考过程"), 1),
                  e.createElementVNode("pre", null, e.toDisplayString(l.reasoning), 1)
                ], 8, ["open"])) : e.createCommentVNode("", !0),
                l.content ? (e.openBlock(), e.createBlock(yt, {
                  key: 1,
                  content: l.content
                }, null, 8, ["content"])) : l.pending ? (e.openBlock(), e.createElementBlock("div", {
                  key: 2,
                  class: "typing"
                }, [
                  e.createElementVNode("span"),
                  e.createElementVNode("span"),
                  e.createElementVNode("span")
                ])) : e.createCommentVNode("", !0)
              ])) : l.role === "tool" ? (e.openBlock(), e.createElementBlock("div", {
                key: 2,
                class: e.normalizeClass(["tool-card", { failed: l.ok === !1 }])
              }, [
                e.createElementVNode("div", { class: "tool-title" }, [
                  e.createElementVNode("span", null, e.toDisplayString(l.pending ? "◌" : l.ok ? "✓" : "!"), 1),
                  e.createElementVNode("strong", null, e.toDisplayString(ve(l.tool)), 1),
                  e.createElementVNode("small", null, e.toDisplayString(l.pending ? "处理中" : l.ok ? "已完成" : "未执行"), 1)
                ]),
                l.approval ? (e.openBlock(), e.createElementBlock("div", {
                  key: 0,
                  class: "approval-box"
                }, [
                  e.createElementVNode("p", null, "该操作需要你的确认："),
                  e.createElementVNode("pre", null, e.toDisplayString(l.approval.arguments), 1),
                  e.createElementVNode("div", { class: "inline-actions" }, [
                    e.createVNode(c, {
                      size: "small",
                      theme: "danger",
                      loading: D.value === l.approval.id,
                      onClick: ($) => de(l, !0)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode("允许")
                      ]),
                      _: 1
                    }, 8, ["loading", "onClick"]),
                    e.createVNode(c, {
                      size: "small",
                      variant: "outline",
                      disabled: !!D.value,
                      onClick: ($) => de(l, !1)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode("拒绝")
                      ]),
                      _: 1
                    }, 8, ["disabled", "onClick"])
                  ])
                ])) : e.createCommentVNode("", !0),
                l.question ? (e.openBlock(), e.createElementBlock("div", {
                  key: 1,
                  class: "question-box"
                }, [
                  e.createElementVNode("p", null, e.toDisplayString(l.question.question), 1),
                  e.createElementVNode("div", { class: "option-list" }, [
                    (e.openBlock(!0), e.createElementBlock(e.Fragment, null, e.renderList(l.question.options, ($) => (e.openBlock(), e.createBlock(c, {
                      key: $,
                      size: "small",
                      variant: "outline",
                      disabled: !!I.value,
                      onClick: (Nt) => ee(l, $)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode(e.toDisplayString($), 1)
                      ]),
                      _: 2
                    }, 1032, ["disabled", "onClick"]))), 128))
                  ]),
                  e.createElementVNode("div", { class: "custom-answer" }, [
                    e.withDirectives(e.createElementVNode("input", {
                      "onUpdate:modelValue": ($) => F[l.question.id] = $,
                      maxlength: "500",
                      placeholder: "或输入自定义回答",
                      onKeydown: e.withKeys(e.withModifiers(($) => ee(l), ["prevent"]), ["enter"])
                    }, null, 40, ["onUpdate:modelValue", "onKeydown"]), [
                      [e.vModelText, F[l.question.id]]
                    ]),
                    e.createVNode(c, {
                      size: "small",
                      loading: I.value === l.question.id,
                      onClick: ($) => ee(l)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode("提交")
                      ]),
                      _: 1
                    }, 8, ["loading", "onClick"])
                  ])
                ])) : e.createCommentVNode("", !0),
                l.content && !l.approval && !l.question ? (e.openBlock(), e.createElementBlock("details", {
                  key: 2,
                  class: "tool-receipt"
                }, [
                  e.createElementVNode("summary", null, "查看工具回执"),
                  e.createElementVNode("pre", null, e.toDisplayString(l.content), 1)
                ])) : e.createCommentVNode("", !0),
                l.diff ? (e.openBlock(), e.createBlock(ft, {
                  key: 3,
                  diff: l.diff
                }, null, 8, ["diff"])) : e.createCommentVNode("", !0)
              ], 2)) : (e.openBlock(), e.createElementBlock("div", {
                key: 3,
                class: "error-bubble"
              }, e.toDisplayString(l.content), 1))
            ], 2))), 128))
          ], 512),
          g.value || w.value ? (e.openBlock(), e.createElementBlock("div", {
            key: 0,
            class: "activity-strip"
          }, [
            g.value ? (e.openBlock(), e.createElementBlock("span", { key: 0 }, e.toDisplayString(g.value), 1)) : e.createCommentVNode("", !0),
            w.value ? (e.openBlock(), e.createElementBlock("span", { key: 1 }, e.toDisplayString(w.value), 1)) : e.createCommentVNode("", !0)
          ])) : e.createCommentVNode("", !0),
          k.value ? (e.openBlock(), e.createElementBlock("div", {
            key: 1,
            class: "error-strip"
          }, e.toDisplayString(k.value), 1)) : e.createCommentVNode("", !0),
          d.value ? e.createCommentVNode("", !0) : (e.openBlock(), e.createElementBlock("div", {
            key: 2,
            class: "error-strip"
          }, "此历史对话的模型或账号权限已变化。请开始新对话。")),
          e.createElementVNode("footer", { class: "composer" }, [
            e.createElementVNode("div", { class: "composer-controls" }, [
              e.createElementVNode("label", null, [
                e.createElementVNode("span", null, "操作模式"),
                e.withDirectives(e.createElementVNode("select", {
                  "onUpdate:modelValue": o[0] || (o[0] = (l) => E.value = l),
                  disabled: f.value
                }, [
                  e.createElementVNode("option", { value: "default" }, "默认确认"),
                  e.createElementVNode("option", { value: "full" }, "完整操作")
                ], 8, ["disabled"]), [
                  [e.vModelSelect, E.value]
                ])
              ]),
              e.createElementVNode("label", { class: "model-select" }, [
                e.createElementVNode("span", null, "模型"),
                e.withDirectives(e.createElementVNode("select", {
                  "onUpdate:modelValue": o[1] || (o[1] = (l) => v.value = l),
                  disabled: f.value || !J.value.length
                }, [
                  e.createElementVNode("option", {
                    value: "",
                    disabled: ""
                  }, "请选择模型"),
                  (e.openBlock(!0), e.createElementBlock(e.Fragment, null, e.renderList(J.value, (l) => (e.openBlock(), e.createElementBlock("option", {
                    key: l.id,
                    value: l.id
                  }, e.toDisplayString(l.name) + " · " + e.toDisplayString(l.model), 9, ["value"]))), 128))
                ], 8, ["disabled"]), [
                  [e.vModelSelect, v.value]
                ])
              ])
            ]),
            e.createElementVNode("div", { class: "input-shell" }, [
              e.withDirectives(e.createElementVNode("textarea", {
                "onUpdate:modelValue": o[2] || (o[2] = (l) => m.value = l),
                maxlength: "4000",
                rows: "3",
                disabled: f.value || !s.value?.ready,
                placeholder: "输入你的需求；Shift+Enter 换行",
                onKeydown: $e
              }, null, 40, ["disabled"]), [
                [e.vModelText, m.value]
              ]),
              e.createElementVNode("div", { class: "input-footer" }, [
                e.createElementVNode("span", null, e.toDisplayString(m.value.length) + " / 4000", 1),
                f.value ? (e.openBlock(), e.createBlock(c, {
                  key: 0,
                  theme: "danger",
                  variant: "outline",
                  size: "small",
                  onClick: _e
                }, {
                  default: e.withCtx(() => [
                    e.createTextVNode("停止")
                  ]),
                  _: 1
                })) : (e.openBlock(), e.createBlock(c, {
                  key: 1,
                  size: "small",
                  disabled: !ie.value,
                  onClick: ue
                }, {
                  default: e.withCtx(() => [
                    e.createTextVNode("发送")
                  ]),
                  _: 1
                }, 8, ["disabled"]))
              ])
            ])
          ])
        ], 64)) : N.value === "history" ? (e.openBlock(), e.createElementBlock("main", {
          key: 2,
          class: "panel-view"
        }, [
          e.createElementVNode("div", { class: "panel-heading" }, [
            e.createElementVNode("div", null, [
              e.createElementVNode("h2", null, "对话历史"),
              e.createElementVNode("p", null, "每个账号最多保留 50 个对话。")
            ]),
            e.createElementVNode("div", { class: "inline-actions" }, [
              e.createVNode(c, {
                size: "small",
                variant: "outline",
                onClick: o[3] || (o[3] = (l) => N.value = "chat")
              }, {
                default: e.withCtx(() => [
                  e.createTextVNode("返回聊天")
                ]),
                _: 1
              }),
              e.createVNode(c, {
                size: "small",
                theme: "danger",
                variant: "outline",
                disabled: !S.value.length,
                onClick: De
              }, {
                default: e.withCtx(() => [
                  e.createTextVNode("删除所选")
                ]),
                _: 1
              }, 8, ["disabled"])
            ])
          ]),
          q.value ? (e.openBlock(), e.createElementBlock("div", {
            key: 0,
            class: "center-state"
          }, [
            e.createVNode(_),
            e.createTextVNode(" 正在读取历史…")
          ])) : H.value.length ? (e.openBlock(), e.createElementBlock("div", {
            key: 2,
            class: "history-list"
          }, [
            (e.openBlock(!0), e.createElementBlock(e.Fragment, null, e.renderList(H.value, (l) => (e.openBlock(), e.createElementBlock("article", {
              key: l.id,
              class: "history-item"
            }, [
              e.createElementVNode("input", {
                type: "checkbox",
                checked: S.value.includes(l.id),
                onChange: (M) => Te(l.id, M.target.checked)
              }, null, 40, ["checked", "onChange"]),
              e.createElementVNode("button", {
                type: "button",
                onClick: (M) => Me(l.id)
              }, [
                e.createElementVNode("strong", null, e.toDisplayString(l.title || "未命名对话"), 1),
                e.createElementVNode("span", null, e.toDisplayString(l.modelName) + " · " + e.toDisplayString(qe(l.updatedAt)), 1)
              ], 8, ["onClick"])
            ]))), 128))
          ])) : (e.openBlock(), e.createElementBlock("div", {
            key: 1,
            class: "center-state"
          }, "还没有保存的对话。"))
        ])) : (e.openBlock(), e.createElementBlock("main", {
          key: 3,
          class: "panel-view settings-view"
        }, [
          e.createElementVNode("div", { class: "panel-heading" }, [
            e.createElementVNode("div", null, [
              e.createElementVNode("h2", null, "Elements AI 设置"),
              e.createElementVNode("p", null, "API Key 只保存在 MSLX 插件数据目录，读取接口不会返回明文。")
            ]),
            e.createVNode(c, {
              size: "small",
              variant: "outline",
              onClick: o[4] || (o[4] = (l) => N.value = "chat")
            }, {
              default: e.withCtx(() => [
                e.createTextVNode("返回聊天")
              ]),
              _: 1
            })
          ]),
          X.value ? (e.openBlock(), e.createElementBlock("div", {
            key: 0,
            class: "center-state"
          }, [
            e.createVNode(_),
            e.createTextVNode(" 正在读取设置…")
          ])) : (e.openBlock(), e.createElementBlock(e.Fragment, { key: 1 }, [
            e.createElementVNode("section", { class: "settings-card" }, [
              e.createElementVNode("div", null, [
                e.createElementVNode("h3", null, "发送偏好"),
                e.createElementVNode("p", null, "开启后按 Enter 发送，Shift+Enter 换行；关闭后使用 Ctrl/⌘+Enter 发送。")
              ]),
              e.createElementVNode("div", { class: "settings-action" }, [
                e.createVNode(Le, {
                  modelValue: A.value,
                  "onUpdate:modelValue": o[5] || (o[5] = (l) => A.value = l)
                }, null, 8, ["modelValue"]),
                e.createVNode(c, {
                  size: "small",
                  loading: Z.value,
                  onClick: Ae
                }, {
                  default: e.withCtx(() => [
                    e.createTextVNode("保存")
                  ]),
                  _: 1
                }, 8, ["loading"])
              ])
            ]),
            e.createElementVNode("section", { class: "model-section" }, [
              e.createElementVNode("div", { class: "section-heading" }, [
                e.createElementVNode("div", null, [
                  e.createElementVNode("h3", null, "个人模型"),
                  e.createElementVNode("p", null, "普通用户只能连接公网模型接口。")
                ]),
                e.createVNode(c, {
                  size: "small",
                  onClick: o[6] || (o[6] = (l) => K("personal"))
                }, {
                  default: e.withCtx(() => [
                    e.createTextVNode("添加模型")
                  ]),
                  _: 1
                })
              ]),
              re.value.length ? (e.openBlock(), e.createElementBlock("div", {
                key: 1,
                class: "model-grid"
              }, [
                (e.openBlock(!0), e.createElementBlock(e.Fragment, null, e.renderList(re.value, (l) => (e.openBlock(), e.createElementBlock("article", {
                  key: l.id,
                  class: "model-card"
                }, [
                  e.createElementVNode("div", null, [
                    e.createElementVNode("strong", null, e.toDisplayString(l.name), 1),
                    e.createElementVNode("span", null, e.toDisplayString(l.model), 1),
                    e.createElementVNode("small", null, e.toDisplayString(l.endpoint), 1)
                  ]),
                  e.createElementVNode("div", { class: "inline-actions" }, [
                    e.createVNode(c, {
                      size: "small",
                      variant: "outline",
                      onClick: (M) => K("personal", l)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode("编辑")
                      ]),
                      _: 1
                    }, 8, ["onClick"]),
                    e.createVNode(c, {
                      size: "small",
                      theme: "danger",
                      variant: "text",
                      onClick: (M) => pe("personal", l)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode("删除")
                      ]),
                      _: 1
                    }, 8, ["onClick"])
                  ])
                ]))), 128))
              ])) : (e.openBlock(), e.createElementBlock("div", {
                key: 0,
                class: "muted-box"
              }, "暂无个人模型。"))
            ]),
            s.value?.admin ? (e.openBlock(), e.createElementBlock("section", {
              key: 0,
              class: "model-section"
            }, [
              e.createElementVNode("div", { class: "section-heading" }, [
                e.createElementVNode("div", null, [
                  e.createElementVNode("h3", null, "管理员预设"),
                  e.createElementVNode("p", null, "所有账号可选择预设模型，但不会看到接口地址和 API Key。")
                ]),
                e.createVNode(c, {
                  size: "small",
                  onClick: o[7] || (o[7] = (l) => K("preset"))
                }, {
                  default: e.withCtx(() => [
                    e.createTextVNode("添加预设")
                  ]),
                  _: 1
                })
              ]),
              L.value.length ? (e.openBlock(), e.createElementBlock("div", {
                key: 1,
                class: "model-grid"
              }, [
                (e.openBlock(!0), e.createElementBlock(e.Fragment, null, e.renderList(L.value, (l) => (e.openBlock(), e.createElementBlock("article", {
                  key: l.id,
                  class: "model-card"
                }, [
                  e.createElementVNode("div", null, [
                    e.createElementVNode("strong", null, e.toDisplayString(l.name), 1),
                    e.createElementVNode("span", null, e.toDisplayString(l.model), 1),
                    e.createElementVNode("small", null, e.toDisplayString(l.endpoint), 1)
                  ]),
                  e.createElementVNode("div", { class: "inline-actions" }, [
                    e.createVNode(c, {
                      size: "small",
                      variant: "outline",
                      onClick: (M) => K("preset", l)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode("编辑")
                      ]),
                      _: 1
                    }, 8, ["onClick"]),
                    e.createVNode(c, {
                      size: "small",
                      theme: "danger",
                      variant: "text",
                      onClick: (M) => pe("preset", l)
                    }, {
                      default: e.withCtx(() => [
                        e.createTextVNode("删除")
                      ]),
                      _: 1
                    }, 8, ["onClick"])
                  ])
                ]))), 128))
              ])) : (e.openBlock(), e.createElementBlock("div", {
                key: 0,
                class: "muted-box"
              }, "暂无管理员预设。"))
            ])) : e.createCommentVNode("", !0)
          ], 64))
        ])),
        e.createVNode(Oe, {
          visible: O.value,
          "onUpdate:visible": o[16] || (o[16] = (l) => O.value = l),
          header: p.id ? "编辑模型" : "添加模型",
          attach: "body",
          width: "560px",
          "confirm-btn": null,
          "cancel-btn": null
        }, {
          default: e.withCtx(() => [
            e.createElementVNode("form", {
              class: "model-form",
              onSubmit: e.withModifiers(Pe, ["prevent"])
            }, [
              e.createElementVNode("label", null, [
                e.createElementVNode("span", null, "显示名称"),
                e.withDirectives(e.createElementVNode("input", {
                  "onUpdate:modelValue": o[8] || (o[8] = (l) => p.name = l),
                  maxlength: "100",
                  required: "",
                  placeholder: "例如：GPT-5"
                }, null, 512), [
                  [e.vModelText, p.name]
                ])
              ]),
              e.createElementVNode("label", null, [
                e.createElementVNode("span", null, "接口地址"),
                e.withDirectives(e.createElementVNode("input", {
                  "onUpdate:modelValue": o[9] || (o[9] = (l) => p.endpoint = l),
                  maxlength: "2048",
                  required: "",
                  placeholder: "https://api.example.com/v1/chat/completions"
                }, null, 512), [
                  [e.vModelText, p.endpoint]
                ])
              ]),
              e.createElementVNode("label", null, [
                e.createElementVNode("span", null, "模型标识"),
                e.withDirectives(e.createElementVNode("input", {
                  "onUpdate:modelValue": o[10] || (o[10] = (l) => p.model = l),
                  maxlength: "200",
                  required: "",
                  placeholder: "模型名称"
                }, null, 512), [
                  [e.vModelText, p.model]
                ])
              ]),
              e.createElementVNode("label", null, [
                e.createElementVNode("span", null, "API Key"),
                e.withDirectives(e.createElementVNode("input", {
                  "onUpdate:modelValue": o[11] || (o[11] = (l) => p.apiKey = l),
                  maxlength: "4096",
                  type: "password",
                  placeholder: p.id ? "留空以保留现有密钥" : "可留空"
                }, null, 8, ["placeholder"]), [
                  [e.vModelText, p.apiKey]
                ])
              ]),
              p.id && p.apiKey === "" ? (e.openBlock(), e.createElementBlock("label", {
                key: 0,
                class: "check-line"
              }, [
                e.withDirectives(e.createElementVNode("input", {
                  "onUpdate:modelValue": o[12] || (o[12] = (l) => p.clearApiKey = l),
                  type: "checkbox"
                }, null, 512), [
                  [e.vModelCheckbox, p.clearApiKey]
                ]),
                e.createTextVNode(" 清除已保存的 API Key")
              ])) : e.createCommentVNode("", !0),
              e.createElementVNode("div", { class: "form-grid" }, [
                e.createElementVNode("label", null, [
                  e.createElementVNode("span", null, "思考模式"),
                  e.withDirectives(e.createElementVNode("select", {
                    "onUpdate:modelValue": o[13] || (o[13] = (l) => P.value = l)
                  }, [
                    e.createElementVNode("option", { value: "default" }, "服务默认"),
                    e.createElementVNode("option", { value: "on" }, "开启"),
                    e.createElementVNode("option", { value: "off" }, "关闭")
                  ], 512), [
                    [e.vModelSelect, P.value]
                  ])
                ]),
                e.createElementVNode("label", null, [
                  e.createElementVNode("span", null, "思考强度"),
                  e.withDirectives(e.createElementVNode("select", {
                    "onUpdate:modelValue": o[14] || (o[14] = (l) => p.thinkingEffort = l),
                    disabled: P.value !== "on"
                  }, [
                    e.createElementVNode("option", { value: "low" }, "低"),
                    e.createElementVNode("option", { value: "medium" }, "中"),
                    e.createElementVNode("option", { value: "high" }, "高")
                  ], 8, ["disabled"]), [
                    [e.vModelSelect, p.thinkingEffort]
                  ])
                ])
              ]),
              e.createElementVNode("div", { class: "dialog-actions" }, [
                e.createVNode(c, {
                  variant: "outline",
                  type: "button",
                  onClick: o[15] || (o[15] = (l) => O.value = !1)
                }, {
                  default: e.withCtx(() => [
                    e.createTextVNode("取消")
                  ]),
                  _: 1
                }),
                e.createVNode(c, {
                  type: "submit",
                  disabled: !ce.value,
                  loading: G.value
                }, {
                  default: e.withCtx(() => [
                    e.createTextVNode("保存")
                  ]),
                  _: 1
                }, 8, ["disabled", "loading"])
              ])
            ], 32)
          ]),
          _: 1
        }, 8, ["visible", "header"])
      ], 2);
    };
  }
}), gt = /* @__PURE__ */ W(ht, [["__scopeId", "data-v-b7b8845e"]]), Et = /* @__PURE__ */ e.defineComponent({
  __name: "InstanceAiDialog",
  props: {
    serverId: {}
  },
  setup(t, { expose: a }) {
    const i = t, r = e.ref(!1), y = e.shallowRef(null), s = e.ref();
    let v;
    const E = e.computed(() => i.serverId ?? s.value);
    function u() {
      r.value = !0;
    }
    function h() {
      const d = window.location.pathname.match(/^\/instance\/console\/(\d+)(?:\/|$)/);
      s.value = d ? Number(d[1]) : void 0;
      const m = document.querySelector(
        ".mslx-webpanel-header-layout .t-menu__operations, .mslx-webpanel-header-layout .t-head-menu__operations"
      ), f = m?.firstElementChild instanceof HTMLElement ? m.firstElementChild : m;
      y.value !== f && (y.value = f);
    }
    return e.onMounted(async () => {
      await e.nextTick(), h(), v = new MutationObserver(h), v.observe(document.body, { childList: !0, subtree: !0 });
    }), e.onBeforeUnmount(() => v?.disconnect()), a({ open: u }), (d, m) => {
      const f = e.resolveComponent("t-button"), b = e.resolveComponent("t-tooltip"), k = e.resolveComponent("t-drawer");
      return e.openBlock(), e.createElementBlock(e.Fragment, null, [
        y.value ? (e.openBlock(), e.createBlock(e.Teleport, {
          key: 0,
          to: y.value
        }, [
          e.createVNode(b, {
            content: "询问 Elements AI",
            placement: "bottom"
          }, {
            default: e.withCtx(() => [
              e.createVNode(f, {
                class: "header-btn elements-ai-header-button",
                theme: "default",
                shape: "square",
                variant: "text",
                "aria-label": "询问 Elements AI",
                onClick: u
              }, {
                default: e.withCtx(() => [
                  e.createVNode(e.unref(Je), { size: "20px" })
                ]),
                _: 1
              })
            ]),
            _: 1
          })
        ], 8, ["to"])) : e.createCommentVNode("", !0),
        e.createVNode(k, {
          visible: r.value,
          "onUpdate:visible": m[0] || (m[0] = (g) => r.value = g),
          header: "Elements AI",
          placement: "right",
          size: "min(720px, 100vw)",
          attach: "body",
          "drawer-class-name": "elements-ai-drawer",
          footer: !1
        }, {
          default: e.withCtx(() => [
            e.createElementVNode("div", { class: "instance-ai-sidebar" }, [
              e.createVNode(gt, {
                "current-instance-id": E.value,
                compact: ""
              }, null, 8, ["current-instance-id"])
            ])
          ]),
          _: 1
        }, 8, ["visible"])
      ], 64);
    };
  }
}), Vt = /* @__PURE__ */ W(Et, [["__scopeId", "data-v-1a108f18"]]), R = "0.1.5", Ve = `mslx-elements-ai-global-root-${R.replaceAll(".", "-")}`, Ne = "__MSLX_ELEMENTS_AI_RUNTIME__";
function Ce() {
  if (typeof document > "u") return;
  const t = window[Ne];
  if (t?.version === R && document.getElementById(Ve)) return;
  t?.unmount(), document.querySelectorAll('[id^="mslx-elements-ai-global-root"]').forEach((r) => r.remove()), document.querySelectorAll(".elements-ai-header-button").forEach((r) => r.remove());
  const a = document.createElement("div");
  a.id = Ve, document.body.appendChild(a);
  const i = e.createApp(Vt);
  i.use(je), i.mount(a), window[Ne] = {
    version: R,
    unmount: () => {
      i.unmount(), a.remove();
    }
  };
}
typeof document < "u" && (document.readyState === "loading" ? document.addEventListener("DOMContentLoaded", Ce, { once: !0 }) : queueMicrotask(Ce));
const Ct = {
  name: "ElementsAI",
  version: R
};
export {
  Ct as pluginConfig
};
