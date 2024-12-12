//Mark
import Bold from "@tiptap/extension-bold";
import Code from "@tiptap/extension-code";
import Highlight from "@tiptap/extension-highlight";
import Italic from "@tiptap/extension-italic";
import Strike from "@tiptap/extension-strike";
import Subscript from "@tiptap/extension-subscript";
import Superscript from "@tiptap/extension-superscript";
import Underline from "@tiptap/extension-underline";
import ExtendedLink from "wysiwyg/model/extensions/link/index";
import FontSize from "wysiwyg/model/extensions/font-size/index";
import LineHeight from "wysiwyg/model/extensions/line-height/index";
import ExtendedHighlight from "wysiwyg/model/extensions/highlight/index";
import TextStyle from '@tiptap/extension-text-style';

// Nodes
import Blockquote from "@tiptap/extension-blockquote";
import BulletList from "@tiptap/extension-bullet-list";
import CodeBlock from "@tiptap/extension-code-block";
import Document from "@tiptap/extension-document";
import Text from "@tiptap/extension-text";
import HardBreak from "@tiptap/extension-hard-break";
import Heading from "@tiptap/extension-heading";
import HorizontalRule from "@tiptap/extension-horizontal-rule";
import ListItem from "@tiptap/extension-list-item";
import OrderedList from "@tiptap/extension-ordered-list";
import Paragraph from "@tiptap/extension-paragraph";
import Table from "@tiptap/extension-table";
import TableCell from "@tiptap/extension-table-cell";
import TableHeader from "@tiptap/extension-table-header";
import TableRow from "@tiptap/extension-table-row";
import Youtube from "@tiptap/extension-youtube";
import Vimeo from "@fourwaves/tiptap-extension-vimeo";
import ExtendedImage from "wysiwyg/model/extensions/image/index";
import Video from "wysiwyg/model/extensions/video/index";

// Functionally
import Color from "@tiptap/extension-color";
import TextAlign from "@tiptap/extension-text-align";
import Typography from "@tiptap/extension-typography";
import History from "@tiptap/extension-history";
import FontFamily from "@tiptap/extension-font-family";
import BubbleMenu from "@tiptap/extension-bubble-menu";
import Gapcursor from '@tiptap/extension-gapcursor';
import RightClickMenu from "wysiwyg/model/extensions/right-click-menu/index";
import CutCopyPaste from "wysiwyg/model/extensions/cut-copy-paste/index";
import SearchAndReplace from "wysiwyg/model/extensions/search-and-replace/index";
import Indent from "wysiwyg/model/extensions/indent/index";
import InvisibleCharacters from "wysiwyg/model/extensions/invisible-characters/index";
import Div from "wysiwyg/model/extensions/div/index";
import Fullscreen from "wysiwyg/model/extensions/fullscreen/index";
import ExtendedCharacterCount from "wysiwyg/model/extensions/characters-count/index";
import Iframe from "wysiwyg/model/extensions/iframe/index";

export const Extensions = {
   Bold, Code, Highlight, Italic, Strike, Subscript, Superscript, Underline, TextStyle,
   Blockquote, BulletList, CodeBlock, Document, Text, HardBreak, Heading, HorizontalRule, ListItem, OrderedList, Paragraph, Table, TableCell, TableHeader, TableRow, Youtube,
   Color, TextAlign, Typography, History, FontFamily, BubbleMenu, Gapcursor,
   ExtendedImage, RightClickMenu, ExtendedLink, FontSize, CutCopyPaste, SearchAndReplace, LineHeight, Indent, InvisibleCharacters, Video, Vimeo, Div, Fullscreen, ExtendedCharacterCount,
   Iframe, ExtendedHighlight,
}