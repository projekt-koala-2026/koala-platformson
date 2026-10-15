import ReactMarkdown, { type Components } from "react-markdown";
import { resolveApiAssetUrl } from "../utils/apiFetcher";

const MarkdownRenderer = ({ content = "" }: { content?: string }) => {
    const components: Components = {
        img: ({ src, alt }) => (
            <img
                src={resolveApiAssetUrl(src)}
                alt={alt ?? ""}
                className="my-4 h-auto max-h-80 max-w-full rounded-xl object-contain"
            />
        ),
        a: ({ href, children }) => (
            <a
                href={href}
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium text-emerald-600 hover:text-emerald-700 hover:underline"
            >
                {children}
            </a>
        ),
    };
    return (
        <div className="markdown-body">
            <ReactMarkdown components={components}>{content}</ReactMarkdown>
        </div>
    );
};
export default MarkdownRenderer;
