import ReactMarkdown from "react-markdown";
import { apiUrl } from "../utils/apiFetcher";

const MarkdownRenderer = ({ content }) => {
    const customComponents = {
        img: ({ src, alt }) => {
            const fullUrl = src.startsWith("http") ? src : `${apiUrl}${src}`;

            return (
                <img
                    src={fullUrl}
                    alt={alt}
                    style={{
                        maxWidth: "50%",
                        height: "auto",
                        maxHeight: "50%",
                        borderRadius: "8px",
                        display: "block",
                        margin: "10px 0",
                    }}
                />
            );
        },
        a: ({ href, children }) => (
            <a href={href} target="_blank" rel="noopener noreferrer">
                {children}
            </a>
        ),
    };

    return (
        <div className="markdownRender">
            <ReactMarkdown components={customComponents}>{content}</ReactMarkdown>
        </div>
    );
};

export default MarkdownRenderer;
