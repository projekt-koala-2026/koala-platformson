import ReactMarkdown, { type Components } from "react-markdown";
import remarkGfm from "remark-gfm";
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
                className="font-medium text-brand-text hover:text-brand-text hover:underline"
            >
                {children}
            </a>
        ),
        table: ({ children }) => (
            <div className="my-5 max-w-full overflow-x-auto">
                <table>{children}</table>
            </div>
        ),
    };
    return (
        <div className="markdown-body">
            <ReactMarkdown remarkPlugins={[remarkGfm]} components={components}>
                {content}
            </ReactMarkdown>
        </div>
    );
};
export default MarkdownRenderer;
